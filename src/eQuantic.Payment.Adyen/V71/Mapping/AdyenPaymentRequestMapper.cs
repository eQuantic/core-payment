using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Adyen.V71.Mapping;

/// <summary>
/// Maps the unified <see cref="CreateChargeRequest"/> to an Adyen <c>POST /v71/payments</c> body.
/// <c>merchantAccount</c> is stamped later by the operations layer (it comes from options, not the request).
/// </summary>
public sealed class AdyenPaymentRequestMapper : IMapper<CreateChargeRequest, AdyenPaymentRequest>
{
    /// <summary>Placeholder return URL sent for action-based methods (PIX, boleto, 3DS cards) that require one.</summary>
    private const string PlaceholderReturnUrl = "https://example.com/return";

    public AdyenPaymentRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var customer = source.Customer;
        var (firstName, lastName) = SplitName(customer?.Name);

        var request = new AdyenPaymentRequest
        {
            Amount = new AdyenAmount { Value = source.Amount.AmountInCents, Currency = source.Amount.Currency },
            // A retry under the same key must send the same reference, so the key stands in for a missing one.
            Reference = source.ReferenceId ?? source.IdempotencyKey ?? Guid.NewGuid().ToString("N"),
            ReturnUrl = PlaceholderReturnUrl,
            PaymentMethod = MapPaymentMethod(source),
            ShopperEmail = customer?.Email,
            SocialSecurityNumber = customer?.DocumentDigits,
            CountryCode = customer?.Address?.Country ?? "BR",
            ShopperStatement = source.Description,
            ShopperName = firstName is null && lastName is null
                ? null
                : new AdyenShopperName { FirstName = firstName, LastName = lastName },
            BillingAddress = customer?.Address is { } address
                ? new AdyenAddress
                {
                    Street = address.Line1,
                    HouseNumberOrName = "S/N",
                    City = address.City,
                    PostalCode = new string(address.ZipCode.Where(char.IsDigit).ToArray()),
                    StateOrProvince = address.State,
                    Country = address.Country,
                }
                : null,
            Metadata = source.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
            case PaymentMethodType.DebitCard:
                var installments = source.Card?.Installments ?? 1;
                request.Installments = new AdyenInstallments { Value = installments > 0 ? installments : 1 };
                break;

            case PaymentMethodType.Pix:
                request.SessionValidity = FormatOffset((source.AttemptedAt ?? DateTimeOffset.UtcNow).Add(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1)));
                break;

            case PaymentMethodType.Boleto:
                if (source.Boleto?.DueDate is { } due)
                {
                    request.DeliveryDate = due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }

                break;
        }

        return request;
    }

    public AdyenPaymentRequest? Map(CreateChargeRequest? source, AdyenPaymentRequest? destination) => Map(source);

    private static AdyenPaymentMethodRequest MapPaymentMethod(CreateChargeRequest request) => request.Method switch
    {
        PaymentMethodType.Pix => new AdyenPaymentMethodRequest { Type = "pix" },
        PaymentMethodType.Boleto => new AdyenPaymentMethodRequest { Type = "boletobancario" },
        _ => MapCard(request),
    };

    private static AdyenPaymentMethodRequest MapCard(CreateChargeRequest request)
    {
        var card = request.Card;

        // A supplied token is treated as an Adyen stored/tokenized payment method.
        if (card?.Token is { } token)
        {
            return new AdyenPaymentMethodRequest
            {
                Type = "scheme",
                StoredPaymentMethodId = token,
                HolderName = card.HolderName,
                Brand = card.Brand,
            };
        }

        return new AdyenPaymentMethodRequest
        {
            Type = "scheme",
            EncryptedCardNumber = card?.Number,
            EncryptedExpiryMonth = card is null ? null : card.ExpirationMonth.ToString("D2", CultureInfo.InvariantCulture),
            EncryptedExpiryYear = card?.ExpirationYear.ToString(CultureInfo.InvariantCulture),
            EncryptedSecurityCode = card?.Cvv,
            HolderName = card?.HolderName,
            Brand = card?.Brand,
        };
    }

    /// <summary>Splits the unified single-field name into Adyen's first/last name.</summary>
    private static (string? First, string? Last) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, null);
        }

        var parts = name.Trim().Split(' ', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], null);
    }

    private static string FormatOffset(DateTimeOffset value)
        => value.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
}
