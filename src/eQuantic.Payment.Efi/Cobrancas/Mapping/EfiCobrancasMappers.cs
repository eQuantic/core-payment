using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Efi.Cobrancas.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Efi.Cobrancas.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to an Efí Cobranças one-step request (boleto or card).</summary>
public sealed class EfiOneStepRequestMapper : IMapper<CreateChargeRequest, EfiOneStepRequest>
{
    public EfiOneStepRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var request = new EfiOneStepRequest
        {
            Items =
            [
                new EfiItem { Name = source.Description ?? "Charge", Value = source.Amount.AmountInCents, Amount = 1 },
            ],
            Metadata = source.ReferenceId is { } r ? new EfiMetadata { CustomId = r } : null,
            Payment = new EfiPaymentRequest(),
        };

        var customer = MapCustomer(source.Customer);

        if (source.Method == PaymentMethodType.Boleto)
        {
            var due = source.Boleto?.DueDate ?? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
            request.Payment.BankingBillet = new EfiBankingBillet
            {
                ExpireAt = due.ToString("yyyy-MM-dd"),
                Customer = customer,
                Message = source.Boleto?.Instructions,
            };
        }
        else
        {
            request.Payment.CreditCard = new EfiCreditCard
            {
                PaymentToken = source.Card?.Token ?? string.Empty,
                Installments = source.Card?.Installments ?? 1,
                Customer = customer,
                BillingAddress = MapAddress(source.Customer?.Address),
            };
        }

        return request;
    }

    public EfiOneStepRequest? Map(CreateChargeRequest? source, EfiOneStepRequest? destination) => Map(source);

    private static EfiCobrancasCustomer MapCustomer(CustomerRequest? customer)
    {
        if (customer is null)
        {
            return new EfiCobrancasCustomer();
        }

        var result = new EfiCobrancasCustomer
        {
            Email = customer.Email,
            PhoneNumber = customer.Phone is null ? null : new string(customer.Phone.Where(char.IsDigit).ToArray()),
            Address = MapAddress(customer.Address),
        };

        if (customer.DocumentType == DocumentType.Cnpj)
        {
            result.JuridicalPerson = new EfiJuridicalPerson { CorporateName = customer.Name, Cnpj = customer.DocumentDigits };
        }
        else
        {
            result.Name = customer.Name;
            result.Cpf = customer.DocumentDigits;
        }

        return result;
    }

    private static EfiCobrancasAddress? MapAddress(AddressRequest? address)
        => address is null
            ? null
            : new EfiCobrancasAddress
            {
                Street = address.Line1,
                Number = "S/N",
                Complement = address.Line2,
                City = address.City,
                State = address.State,
                Zipcode = new string((address.ZipCode ?? string.Empty).Where(char.IsDigit).ToArray()),
            };
}

/// <summary>Maps an Efí Cobranças charge to the unified <see cref="Charge"/>.</summary>
public sealed class EfiCobrancasChargeMapper : IMapper<EfiChargeData, Charge>
{
    public Charge? Map(EfiChargeData? source)
    {
        if (source is null)
        {
            return null;
        }

        var method = source.Payment?.ToLowerInvariant() switch
        {
            "banking_billet" => PaymentMethodType.Boleto,
            "credit_card" => PaymentMethodType.CreditCard,
            _ => PaymentMethodType.Boleto,
        };

        return new Charge
        {
            Id = source.ChargeId.ToString(),
            Status = EfiStatusMapper.FromCobrancas(source.Status),
            ProviderStatus = source.Status,
            Amount = Money.FromCents(source.Total),
            Method = method,
            CreatedAt = ParseCreatedAt(source.CreatedAt),
            Boleto = method == PaymentMethodType.Boleto
                ? new BoletoOutput
                {
                    Barcode = source.Barcode,
                    DigitableLine = source.Barcode,
                    Url = source.Pdf?.Charge ?? source.BilletLink ?? source.Link,
                    DueDate = ParseDate(source.ExpireAt),
                }
                : null,
            Card = method == PaymentMethodType.CreditCard
                ? new CardOutput { Installments = source.Installments is null or <= 0 ? 1 : source.Installments.Value }
                : null,
        };
    }

    public Charge? Map(EfiChargeData? source, Charge? destination) => Map(source);

    private static DateOnly? ParseDate(string? date)
        => DateOnly.TryParse(date, CultureInfo.InvariantCulture, out var d) ? d : null;

    private static DateTimeOffset? ParseCreatedAt(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : null;
}

/// <summary>Maps the Efí Cobranças charge returned by a refund to the unified <see cref="Refund"/>.</summary>
public sealed class EfiCobrancasRefundMapper : IMapper<EfiChargeData, Refund>
{
    public Refund? Map(EfiChargeData? source)
        => source is null
            ? null
            : new Refund
            {
                Id = source.ChargeId.ToString(),
                ChargeId = source.ChargeId.ToString(),
                Amount = Money.FromCents(source.Total),
                Status = EfiStatusMapper.FromCobrancas(source.Status),
                ProviderStatus = source.Status,
            };

    public Refund? Map(EfiChargeData? source, Refund? destination) => Map(source);
}
