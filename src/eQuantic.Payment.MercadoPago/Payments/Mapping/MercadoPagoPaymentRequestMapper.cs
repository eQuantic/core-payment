using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Payments.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.MercadoPago.Payments.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to a Mercado Pago Payments API request.</summary>
public sealed class MercadoPagoPaymentRequestMapper : IMapper<CreateChargeRequest, MercadoPagoPaymentRequest>
{
    public MercadoPagoPaymentRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var request = new MercadoPagoPaymentRequest
        {
            // Mercado Pago expects the amount in decimal currency units, not centavos.
            TransactionAmount = source.Amount.Amount,
            PaymentMethodId = MercadoPagoPaymentWire.ToPaymentMethodId(source.Method, source.Card?.Brand),
            Description = source.Description,
            ExternalReference = source.ReferenceId,
            Payer = MapPayer(source.Customer),
            Metadata = source.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
            case PaymentMethodType.DebitCard:
                request.Token = source.Card?.Token;
                request.Installments = source.Card?.Installments ?? 1;
                request.Capture = source.Capture;
                break;

            case PaymentMethodType.Pix:
                request.DateOfExpiration = (source.AttemptedAt ?? DateTimeOffset.UtcNow).Add(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1));
                break;

            case PaymentMethodType.Boleto:
                request.DateOfExpiration = source.Boleto?.DueDate is { } due
                    ? new DateTimeOffset(due.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3)) // America/Sao_Paulo
                    : null;
                break;
        }

        return request;
    }

    public MercadoPagoPaymentRequest? Map(CreateChargeRequest? source, MercadoPagoPaymentRequest? destination) => Map(source);

    private static MercadoPagoPayerRequest MapPayer(CustomerRequest? customer)
    {
        if (customer is null)
        {
            return new MercadoPagoPayerRequest();
        }

        var (first, last) = MercadoPagoConventions.SplitName(customer.Name);
        return new MercadoPagoPayerRequest
        {
            Email = customer.Email,
            FirstName = first,
            LastName = last,
            Identification = MercadoPagoConventions.ToIdentification(customer),
            Phone = MercadoPagoConventions.ToPhone(customer.Phone),
            Address = customer.Address is { } a
                ? new MercadoPagoPayerAddress
                {
                    ZipCode = a.ZipCode,
                    StreetName = a.Line1,
                    City = a.City,
                    FederalUnit = a.State,
                }
                : null,
        };
    }
}
