using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to an Asaas v3 payment request.</summary>
public sealed class AsaasPaymentRequestMapper : IMapper<CreateChargeRequest, AsaasPaymentRequest>
{
    /// <summary>Fallback window (days from today) for the required <c>dueDate</c> when none is supplied.</summary>
    private const int DefaultDueDateDays = 3;

    public AsaasPaymentRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var request = new AsaasPaymentRequest
        {
            BillingType = AsaasPaymentWire.ToBillingType(source.Method),

            // Asaas expects the amount in decimal reais, not centavos.
            Value = source.Amount.Amount,
            DueDate = ResolveDueDate(source).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Description = source.Description,
            ExternalReference = source.ReferenceId,
        };

        if (source.Method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard)
        {
            MapCard(source, request);
        }

        return request;
    }

    public AsaasPaymentRequest? Map(CreateChargeRequest? source, AsaasPaymentRequest? destination) => Map(source);

    private static DateOnly ResolveDueDate(CreateChargeRequest source)
        => source.Boleto?.DueDate ?? DateOnly.FromDateTime((source.AttemptedAt ?? DateTimeOffset.UtcNow).UtcDateTime).AddDays(DefaultDueDateDays);

    private static void MapCard(CreateChargeRequest source, AsaasPaymentRequest request)
    {
        var card = source.Card;
        request.RemoteIp = Metadata(source, "remoteIp");

        // Capture == false means authorize only; capture happens later via captureAuthorizedPayment.
        if (!source.Capture)
        {
            request.AuthorizeOnly = true;
        }

        if (card is { Installments: > 1 })
        {
            request.InstallmentCount = card.Installments;
        }

        // A stored token replaces the raw card data and holder info.
        if (card is not null && !string.IsNullOrWhiteSpace(card.Token))
        {
            request.CreditCardToken = card.Token;
            return;
        }

        request.CreditCard = new AsaasCreditCard
        {
            HolderName = card?.HolderName ?? source.Customer?.Name,
            Number = card?.Number,
            ExpiryMonth = card is null ? null : card.ExpirationMonth.ToString("D2", CultureInfo.InvariantCulture),
            ExpiryYear = NormalizeYear(card?.ExpirationYear),
            Ccv = card?.Cvv,
        };
        request.CreditCardHolderInfo = MapHolderInfo(source);
    }

    private static AsaasCreditCardHolderInfo MapHolderInfo(CreateChargeRequest source)
    {
        var customer = source.Customer;
        return new AsaasCreditCardHolderInfo
        {
            Name = source.Card?.HolderName ?? customer?.Name,
            Email = customer?.Email,
            CpfCnpj = customer?.DocumentDigits,
            PostalCode = customer?.Address?.ZipCode,
            AddressNumber = Metadata(source, "addressNumber"),
            AddressComplement = customer?.Address?.Line2,
            Phone = customer?.Phone,
            MobilePhone = Metadata(source, "mobilePhone"),
        };
    }

    private static string? NormalizeYear(int? year)
    {
        if (year is not { } value || value <= 0)
        {
            return null;
        }

        if (value < 100)
        {
            value += 2000;
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string? Metadata(CreateChargeRequest source, string key)
        => source.Metadata is not null && source.Metadata.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
}
