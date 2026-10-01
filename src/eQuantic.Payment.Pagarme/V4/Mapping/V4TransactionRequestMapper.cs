using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to a legacy Pagar.me v4 transaction request.</summary>
public sealed class V4TransactionRequestMapper : IMapper<CreateChargeRequest, V4TransactionRequest>
{
    public V4TransactionRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var tx = new V4TransactionRequest
        {
            Amount = source.Amount.AmountInCents,
            PaymentMethod = PagarmeV4Wire.ToPaymentMethod(source.Method),
            ReferenceKey = source.ReferenceId,
            Customer = source.Customer is null ? null : MapCustomer(source.Customer),
            Metadata = source.Metadata?.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        switch (source.Method)
        {
            case PaymentMethodType.CreditCard:
            case PaymentMethodType.DebitCard:
                tx.Installments = source.Card?.Installments ?? 1;
                tx.Capture = source.Capture;
                if (source.Card?.Token is { } token)
                {
                    tx.CardId = token;
                }
                else if (source.Card is not null)
                {
                    tx.CardNumber = source.Card.Number;
                    tx.CardHolderName = source.Card.HolderName;
                    tx.CardExpirationDate = PagarmeV4Wire.ToExpirationDate(source.Card.ExpirationMonth, source.Card.ExpirationYear);
                    tx.CardCvv = source.Card.Cvv;
                }
                break;

            case PaymentMethodType.Pix:
                tx.PixExpirationDate = (source.AttemptedAt ?? DateTimeOffset.UtcNow).Add(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1));
                break;

            case PaymentMethodType.Boleto:
                tx.BoletoExpirationDate = source.Boleto?.DueDate is { } due
                    ? new DateTimeOffset(due.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                    : null;
                tx.BoletoInstructions = source.Boleto?.Instructions;
                break;
        }

        return tx;
    }

    public V4TransactionRequest? Map(CreateChargeRequest? source, V4TransactionRequest? destination) => Map(source);

    private static V4Customer MapCustomer(CustomerRequest request) => new()
    {
        ExternalId = request.Email,
        Name = request.Name,
        Email = request.Email,
        Type = request.DocumentType == eQuantic.Payment.Models.Requests.DocumentType.Cnpj ? "corporation" : "individual",
        Documents = request.DocumentDigits is { } doc
            ?
            [
                new V4Document { Type = request.DocumentType == eQuantic.Payment.Models.Requests.DocumentType.Cnpj ? "cnpj" : "cpf", Number = doc },
            ]
            : null,
        PhoneNumbers = request.Phone is null ? null : [request.Phone],
    };
}
