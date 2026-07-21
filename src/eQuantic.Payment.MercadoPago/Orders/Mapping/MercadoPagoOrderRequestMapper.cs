using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Orders.Models;
using eQuantic.Payment.MercadoPago.Payments.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.MercadoPago.Orders.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to a Mercado Pago Orders API request.</summary>
public sealed class MercadoPagoOrderRequestMapper : IMapper<CreateChargeRequest, MercadoPagoOrderRequest>
{
    public MercadoPagoOrderRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var amount = MercadoPagoOrderWire.ToAmount(source.Amount.Amount);
        var (methodId, methodType) = MercadoPagoOrderWire.ToPaymentMethod(source.Method, source.Card?.Brand);

        var payment = new MercadoPagoOrderPaymentRequest
        {
            Amount = amount,
            PaymentMethod = new MercadoPagoOrderPaymentMethodRequest { Id = methodId, Type = methodType },
        };

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
            case PaymentMethodType.DebitCard:
                payment.PaymentMethod.Token = source.Card?.Token;
                payment.PaymentMethod.Installments = source.Card?.Installments ?? 1;
                break;

            case PaymentMethodType.Pix:
                payment.DateOfExpiration = DateTimeOffset.UtcNow.Add(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1));
                break;

            case PaymentMethodType.Boleto:
                payment.DateOfExpiration = source.Boleto?.DueDate is { } due
                    ? new DateTimeOffset(due.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(-3))
                    : null;
                break;
        }

        return new MercadoPagoOrderRequest
        {
            Type = "online",
            TotalAmount = amount,
            ExternalReference = source.ReferenceId,
            ProcessingMode = "automatic",
            CaptureMode = source.Capture ? "automatic" : "manual",
            Currency = source.Amount.Currency,
            Description = source.Description,
            Transactions = new MercadoPagoOrderTransactionsRequest { Payments = [payment] },
            Payer = MapPayer(source.Customer),
        };
    }

    public MercadoPagoOrderRequest? Map(CreateChargeRequest? source, MercadoPagoOrderRequest? destination) => Map(source);

    private static MercadoPagoOrderPayerRequest? MapPayer(CustomerRequest? customer)
    {
        if (customer is null)
        {
            return null;
        }

        var (first, last) = MercadoPagoConventions.SplitName(customer.Name);
        return new MercadoPagoOrderPayerRequest
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
