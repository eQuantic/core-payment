using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Maps a unified <see cref="CreateChargeRequest"/> to the Stripe <c>POST /v1/payment_intents</c> form body.
/// Uses <see cref="StripeRequestContext.Today"/> to compute the boleto <c>expires_after_days</c>.
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
        var form = new StripeForm
        {
            { "amount", source.Amount.AmountInCents.ToString() },
            { "currency", source.Amount.Currency.ToLowerInvariant() },
            { "confirm", "true" },
        };

        var method = ToPaymentMethodType(source.Method);
        form.Add("payment_method_types[]", method);

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
                if (SavedPaymentMethod(source.Card) is { } paymentMethodId)
                {
                    form.Add("payment_method", paymentMethodId);
                }
                else
                {
                    form.Add("payment_method_data[type]", method);
                    if (source.Card?.Token is { } token)
                    {
                        form.Add("payment_method_data[card][token]", token);
                    }
                }

                var installments = source.Card?.Installments ?? 1;
                if (installments > 1)
                {
                    form.Add("payment_method_options[card][installments][enabled]", "true");
                    form.Add("payment_method_options[card][installments][plan][count]", installments.ToString());
                    form.Add("payment_method_options[card][installments][plan][interval]", "month");
                    form.Add("payment_method_options[card][installments][plan][type]", "fixed_count");
                }
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
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };
}
