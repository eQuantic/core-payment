using eQuantic.Mapper;
using eQuantic.Payment.Cielo.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Cielo.V3.Mapping;

/// <summary>
/// Maps the unified <see cref="CreateChargeRequest"/> to a Cielo create-sale request. Card data goes into
/// <c>Payment.CreditCard</c>/<c>Payment.DebitCard</c>; PIX and boleto are expressed via <c>Payment.Type</c>.
/// </summary>
public sealed class CieloSaleRequestMapper : IMapper<CreateChargeRequest, CieloSaleRequest>
{
    public CieloSaleRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var payment = new CieloPaymentRequest
        {
            Type = CieloV3Wire.ToPaymentType(source.Method),
            Amount = source.Amount.AmountInCents,
        };

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
                payment.Installments = source.Card?.Installments ?? 1;
                payment.Capture = source.Capture;
                payment.SoftDescriptor = ToSoftDescriptor(source.Description);
                payment.CreditCard = MapCard(source.Card);
                break;

            case PaymentMethodType.DebitCard:
                payment.SoftDescriptor = ToSoftDescriptor(source.Description);
                payment.DebitCard = MapCard(source.Card);
                break;

            case PaymentMethodType.Boleto:
                payment.ExpirationDate = source.Boleto?.DueDate?.ToString("yyyy-MM-dd");
                payment.Instructions = source.Boleto?.Instructions;
                break;

            case PaymentMethodType.Pix:
            default:
                break;
        }

        return new CieloSaleRequest
        {
            MerchantOrderId = string.IsNullOrWhiteSpace(source.ReferenceId)
                ? Guid.NewGuid().ToString("N")
                : source.ReferenceId,
            Customer = MapCustomer(source.Customer),
            Payment = payment,
        };
    }

    public CieloSaleRequest? Map(CreateChargeRequest? source, CieloSaleRequest? destination) => Map(source);

    private static CieloCardRequest? MapCard(CardDetails? card)
    {
        if (card is null)
        {
            return null;
        }

        // A token references a previously stored card; otherwise send the raw PAN fields.
        if (card.Token is { } token)
        {
            return new CieloCardRequest { CardToken = token, Brand = card.Brand };
        }

        return new CieloCardRequest
        {
            CardNumber = card.Number,
            Holder = card.HolderName,
            ExpirationDate = $"{card.ExpirationMonth:D2}/{card.ExpirationYear:D4}",
            SecurityCode = card.Cvv,
            Brand = card.Brand,
        };
    }

    private static CieloCustomerRequest? MapCustomer(CustomerRequest? customer)
    {
        if (customer is null)
        {
            return null;
        }

        return new CieloCustomerRequest
        {
            Name = customer.Name,
            Email = customer.Email,
            Identity = customer.DocumentDigits,
            IdentityType = customer.DocumentType switch
            {
                DocumentType.Cpf => "CPF",
                DocumentType.Cnpj => "CNPJ",
                _ => null,
            },
        };
    }

    private static string? ToSoftDescriptor(string? description)
        => description is { Length: > 0 } d ? d[..Math.Min(13, d.Length)] : null;
}
