using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Asaas.V3.Mapping;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Asaas.V3;

/// <summary>
/// Charge and refund operations over the Asaas v3 API (<c>/payments</c>). Because Asaas requires an existing
/// customer to create a payment, <see cref="CreateAsync(CreateChargeRequest, CancellationToken)"/> first creates
/// the customer (when customer data is supplied) and then the payment. PIX QR code and boleto identification
/// field are fetched via follow-up GET calls to populate the unified <see cref="Charge"/>.
/// </summary>
internal sealed class AsaasV3Operations(AsaasV3Client client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, AsaasPaymentRequest>().Map(request)!;

        // Asaas /payments requires a customer id. The unified request carries customer DATA, so create the
        // customer first and forward its id.
        if (request.Customer is not null)
        {
            var customerBody = mappers.GetMapper<CustomerRequest, AsaasCustomerRequest>().Map(request.Customer)!;
            var customerResult = await client.CreateCustomerAsync(customerBody, cancellationToken).ConfigureAwait(false);
            if (!customerResult.IsSuccess || customerResult.Data is null)
            {
                return PaymentResponse<Charge>.Fail(info, AsaasErrorMapper.ToError(customerResult), customerResult.RawBody);
            }

            body.Customer = customerResult.Data.Id;
        }

        var result = await client.CreatePaymentAsync(body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, AsaasErrorMapper.ToError(result), result.RawBody);
        }

        return await BuildChargeAsync(result.Data, result.RawBody, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetPaymentAsync(chargeId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, AsaasErrorMapper.ToError(result), result.RawBody);
        }

        return await BuildChargeAsync(result.Data, result.RawBody, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Captures a previously authorized card charge. Asaas captures the full authorized amount, so
    /// <paramref name="amount"/> (partial capture) is not supported and is ignored.
    /// </summary>
    public async Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
    {
        var result = await client.CaptureAuthorizedPaymentAsync(chargeId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, AsaasErrorMapper.ToError(result), result.RawBody);
        }

        return await BuildChargeAsync(result.Data, result.RawBody, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.DeletePaymentAsync(chargeId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, AsaasErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<AsaasDeleteResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        var body = request.Amount is { } amount ? new AsaasRefundRequest { Value = amount.Amount } : null;
        var result = await client.RefundPaymentAsync(request.ChargeId, body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, AsaasErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<AsaasPaymentResponse, Refund>().Map(result.Data)!;
        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private async Task<PaymentResponse<Charge>> BuildChargeAsync(AsaasPaymentResponse payment, string? rawBody, CancellationToken cancellationToken)
    {
        AsaasPixQrCodeResponse? pixQrCode = null;
        AsaasIdentificationFieldResponse? identificationField = null;

        // PIX QR code and boleto line are separate follow-up GET calls, not part of the payment object.
        var method = AsaasPaymentWire.FromBillingType(payment.BillingType);
        if (method == PaymentMethodType.Pix)
        {
            var pixResult = await client.GetPixQrCodeAsync(payment.Id, cancellationToken).ConfigureAwait(false);
            pixQrCode = pixResult.IsSuccess ? pixResult.Data : null;
        }
        else if (method == PaymentMethodType.Boleto)
        {
            var identificationResult = await client.GetIdentificationFieldAsync(payment.Id, cancellationToken).ConfigureAwait(false);
            identificationField = identificationResult.IsSuccess ? identificationResult.Data : null;
        }

        var source = new AsaasPaymentResult
        {
            Payment = payment,
            PixQrCode = pixQrCode,
            IdentificationField = identificationField,
        };

        var charge = mappers.GetMapper<AsaasPaymentResult, Charge>().Map(source)!;
        return PaymentResponse<Charge>.Ok(info, charge, rawBody);
    }
}
