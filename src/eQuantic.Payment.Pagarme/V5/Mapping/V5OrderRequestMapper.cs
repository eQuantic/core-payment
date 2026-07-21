using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to a Pagar.me v5 order request.</summary>
public sealed class V5OrderRequestMapper(IMapperFactory mapperFactory) : IMapper<CreateChargeRequest, V5OrderRequest>
{
    public V5OrderRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var payment = new V5PaymentRequest { PaymentMethod = PagarmeV5Wire.ToPaymentMethod(source.Method) };

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
                payment.CreditCard = new V5CreditCardPaymentRequest
                {
                    OperationType = source.Capture ? "auth_and_capture" : "auth_only",
                    Installments = source.Card?.Installments ?? 1,
                    StatementDescriptor = source.Description,
                    CardToken = source.Card?.Token,
                    Card = MapCard(source.Card),
                };
                break;

            case PaymentMethodType.DebitCard:
                payment.DebitCard = new V5DebitCardPaymentRequest
                {
                    StatementDescriptor = source.Description,
                    CardToken = source.Card?.Token,
                    Card = MapCard(source.Card),
                };
                break;

            case PaymentMethodType.Pix:
                payment.Pix = new V5PixPaymentRequest
                {
                    ExpiresIn = (int)(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1)).TotalSeconds,
                };
                break;

            case PaymentMethodType.Boleto:
                payment.Boleto = new V5BoletoPaymentRequest
                {
                    DueAt = source.Boleto?.DueDate is { } due ? new DateTimeOffset(due.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) : null,
                    Instructions = source.Boleto?.Instructions,
                };
                break;
        }

        return new V5OrderRequest
        {
            Code = source.ReferenceId,
            Closed = true,
            Items =
            [
                new V5OrderItemRequest
                {
                    Amount = source.Amount.AmountInCents,
                    Description = source.Description ?? "Charge",
                    Quantity = 1,
                    Code = source.ReferenceId,
                },
            ],
            Customer = source.Customer is null
                ? null
                : mapperFactory.GetMapper<CustomerRequest, V5CustomerRequest>().Map(source.Customer),
            Payments = [payment],
            Metadata = source.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
        };
    }

    public V5OrderRequest? Map(CreateChargeRequest? source, V5OrderRequest? destination) => Map(source);

    private static V5CardRequest? MapCard(CardDetails? card)
        => card is null || card.Token is not null
            ? null
            : new V5CardRequest
            {
                Number = card.Number,
                HolderName = card.HolderName,
                ExpMonth = card.ExpirationMonth,
                ExpYear = card.ExpirationYear,
                Cvv = card.Cvv,
            };
}
