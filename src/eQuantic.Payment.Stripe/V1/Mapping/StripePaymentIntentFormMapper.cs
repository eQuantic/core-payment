using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Maps a unified <see cref="CreateChargeRequest"/> to the Stripe <c>POST /v1/payment_intents</c> form body: a card,
/// a saved card, Pix, boleto, MB WAY or Multibanco, each confirmed at once, or a card with no card to charge, left
/// for the customer's browser to confirm (<see cref="ConfirmsInBrowser"/>).
/// Uses <see cref="StripeRequestContext.Today"/> to compute the boleto <c>expires_after_days</c>, and
/// <see cref="StripeRequestContext.Version"/> to name the payment method types field.
/// </summary>
public sealed class StripePaymentIntentFormMapper : IMapper<CreateChargeRequest, StripeForm, StripeRequestContext>
{
    /// <summary>Stripe boleto <c>expires_after_days</c> is clamped to this documented range (0–60).</summary>
    private const int BoletoMaxDays = 60;

    public StripeRequestContext? Context { get; set; }

    public StripeForm? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var today = Context?.Today ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var inBrowser = ConfirmsInBrowser(source);
        var form = new StripeForm
        {
            { "amount", source.Amount.AmountInCents.ToString() },
            { "currency", source.Amount.Currency.ToLowerInvariant() },
        };

        // The browser confirms what it collects (Stripe.js confirmPayment); everything else is confirmed here, at once.
        if (!inBrowser)
        {
            form.Add("confirm", "true");
        }

        var method = ToPaymentMethodType(source.Method);
        form.Add(StripeDefaults.PaymentMethodTypesField(Context?.Version ?? StripeDefaults.Version), method);

        if (source.CustomerId is { } customerId)
        {
            form.Add("customer", customerId);
        }

        if (!string.IsNullOrWhiteSpace(source.Description))
        {
            form.Add("description", source.Description);
        }

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
            case PaymentMethodType.DebitCard:
                form.Add("capture_method", source.Capture ? "automatic" : "manual");

                // In the browser no payment method is sent: the Payment Element collects one (a card, Apple Pay, Google Pay).
                if (!inBrowser)
                {
                    AppendCard(form, source.Card, method);
                }

                AppendInstallments(form, source.Card, inBrowser);
                break;

            case PaymentMethodType.Pix:
                form.Add("payment_method_data[type]", method);
                var seconds = (int)(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1)).TotalSeconds;
                form.Add("payment_method_options[pix][expires_after_seconds]", seconds.ToString());
                break;

            case PaymentMethodType.Boleto:
                form.Add("payment_method_data[type]", method);
                AppendBoleto(form, source, today);
                break;

            // Neither takes manual capture, so no capture method is sent: Stripe's default, automatic, is the only one.
            case PaymentMethodType.MbWay:
                form.Add("payment_method_data[type]", method);
                if (source.MbWay?.InternationalPhoneNumber is { } phone)
                {
                    form.Add("payment_method_data[billing_details][phone]", phone);
                }

                AppendPayer(form, source.Customer);
                break;

            case PaymentMethodType.Multibanco:
                form.Add("payment_method_data[type]", method);
                AppendPayer(form, source.Customer);
                break;
        }

        // With the customer away, Stripe declines what would ask for authentication rather than wait for it.
        if (source.OffSession)
        {
            form.Add("off_session", "true");
        }

        if (source.Customer?.Email is { } email)
        {
            form.Add("receipt_email", email);
        }

        if (source.ReferenceId is { } reference)
        {
            form.Add("metadata[reference_id]", reference);
        }

        if (source.Metadata is not null)
        {
            foreach (var (key, value) in source.Metadata)
            {
                form.Add($"metadata[{key}]", value);
            }
        }

        return form;
    }

    public StripeForm? Map(CreateChargeRequest? source, StripeForm? destination) => Map(source);

    /// <summary>
    /// The saved payment method to charge: <see cref="CardDetails.PaymentMethodId"/>, or a <c>pm_</c> id passed as
    /// <see cref="CardDetails.Token"/>, which 1.x documented as accepted there.
    /// </summary>
    internal static string? SavedPaymentMethod(CardDetails? card) =>
        card?.PaymentMethodId ?? (card?.Token is { } token && token.StartsWith("pm_", StringComparison.Ordinal) ? token : null);

    /// <summary>
    /// Whether the customer's browser confirms the charge: a card charge with the customer present and no card to
    /// charge, neither a token, a saved payment method nor a card number. Stripe creates its PaymentIntent unconfirmed,
    /// and Stripe.js confirms it with what the Payment Element collects. A card number with no token is not one: the
    /// caller meant that card, and Stripe refuses it as before.
    /// </summary>
    internal static bool ConfirmsInBrowser(CreateChargeRequest request) =>
        request.Method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
        && !request.OffSession
        && string.IsNullOrWhiteSpace(request.Card?.Token)
        && string.IsNullOrWhiteSpace(request.Card?.PaymentMethodId)
        && string.IsNullOrWhiteSpace(request.Card?.Number);

    /// <summary>The card a charge confirmed here is for: a saved payment method, or the card behind a token.</summary>
    private static void AppendCard(StripeForm form, CardDetails? card, string method)
    {
        if (SavedPaymentMethod(card) is { } paymentMethodId)
        {
            form.Add("payment_method", paymentMethodId);
            return;
        }

        form.Add("payment_method_data[type]", method);
        if (card?.Token is { } token)
        {
            form.Add("payment_method_data[card][token]", token);
        }
    }

    /// <summary>
    /// More than one installment: offered, and with the plan when the charge is confirmed here. Stripe takes a plan only
    /// when the payment is confirmed, so in the browser the customer picks it in the Payment Element.
    /// </summary>
    private static void AppendInstallments(StripeForm form, CardDetails? card, bool inBrowser)
    {
        var installments = card?.Installments ?? 1;
        if (installments <= 1)
        {
            return;
        }

        form.Add("payment_method_options[card][installments][enabled]", "true");
        if (!inBrowser)
        {
            form.Add("payment_method_options[card][installments][plan][count]", installments.ToString());
            form.Add("payment_method_options[card][installments][plan][interval]", "month");
            form.Add("payment_method_options[card][installments][plan][type]", "fixed_count");
        }
    }

    private static void AppendBoleto(StripeForm form, CreateChargeRequest request, DateOnly today)
    {
        if (request.Customer?.DocumentDigits is { } taxId)
        {
            form.Add("payment_method_data[boleto][tax_id]", taxId);
        }

        // Stripe boleto requires full billing_details (name, email, address), the name and address in ASCII.
        if (request.Customer is { } customer)
        {
            form.Add("payment_method_data[billing_details][name]", StripeBoleto.Ascii(customer.Name));
            form.Add("payment_method_data[billing_details][email]", customer.Email);
            AppendAddress(form, "payment_method_data[billing_details][address]", customer.Address, StripeBoleto.Ascii);
        }

        // Boleto expiry is expressed in whole days from São Paulo's today (0–60), not an absolute timestamp.
        if (request.Boleto?.DueDate is { } due)
        {
            var days = Math.Clamp(due.DayNumber - today.DayNumber, 0, BoletoMaxDays);
            form.Add("payment_method_options[boleto][expires_after_days]", days.ToString());
        }
    }

    /// <summary>The payer's name and email, when there is one: Multibanco needs the email, and MB WAY takes both.</summary>
    private static void AppendPayer(StripeForm form, CustomerRequest? payer)
    {
        if (!string.IsNullOrWhiteSpace(payer?.Name))
        {
            form.Add("payment_method_data[billing_details][name]", payer.Name);
        }

        if (!string.IsNullOrWhiteSpace(payer?.Email))
        {
            form.Add("payment_method_data[billing_details][email]", payer.Email);
        }
    }

    internal static void AppendAddress(StripeForm form, string prefix, AddressRequest? address, Func<string, string>? text = null)
    {
        if (address is null)
        {
            return;
        }

        text ??= value => value;
        form.Add($"{prefix}[line1]", text(address.Line1));
        if (address.Line2 is { } line2)
        {
            form.Add($"{prefix}[line2]", text(line2));
        }

        form.Add($"{prefix}[city]", text(address.City));
        form.Add($"{prefix}[state]", text(address.State));
        form.Add($"{prefix}[postal_code]", address.ZipCode);
        form.Add($"{prefix}[country]", address.Country);
    }

    private static string ToPaymentMethodType(PaymentMethodType method) => method switch
    {
        PaymentMethodType.CreditCard => "card",
        PaymentMethodType.DebitCard => "card",
        PaymentMethodType.Pix => "pix",
        PaymentMethodType.Boleto => "boleto",
        PaymentMethodType.MbWay => "mb_way",
        PaymentMethodType.Multibanco => "multibanco",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };
}
