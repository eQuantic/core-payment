using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>
/// Cards saved for a customer through a SetupIntent, which a front end confirms with Stripe.js, so no card number
/// reaches the caller; listed and detached through the customer's PaymentMethods.
/// </summary>
internal sealed class StripePaymentMethodOperations(StripeClientV1 client, ProviderInfo info, IMapperFactory mappers)
    : IPaymentMethodOperations
{
    public async Task<PaymentResponse<PaymentMethodSetup>> SetupAsync(PaymentMethodSetupRequest request, CancellationToken cancellationToken = default)
    {
        var form = mappers.GetMapper<PaymentMethodSetupRequest, StripeForm>().Map(request)!;
        var result = await client.CreateSetupIntentAsync(form, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapSetup(result);
    }

    public async Task<PaymentResponse<PaymentMethodSetup>> GetSetupAsync(string setupId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetSetupIntentAsync(setupId, cancellationToken).ConfigureAwait(false);
        return MapSetup(result);
    }

    public async Task<PaymentResponse<IReadOnlyList<SavedPaymentMethod>>> ListAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var result = await client.ListCustomerPaymentMethodsAsync(customerId, "card", cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<IReadOnlyList<SavedPaymentMethod>>.Fail(info, StripeErrorMapper.ToError(result), result.RawBody);
        }

        var mapper = mappers.GetMapper<StripePaymentMethod, SavedPaymentMethod>();
        IReadOnlyList<SavedPaymentMethod> saved = [.. result.Data.Data.Select(method => mapper.Map(method)!)];
        return PaymentResponse<IReadOnlyList<SavedPaymentMethod>>.Ok(info, saved, result.RawBody);
    }

    public async Task<PaymentResponse<SavedPaymentMethod>> DetachAsync(string paymentMethodId, CancellationToken cancellationToken = default)
    {
        var result = await client.DetachPaymentMethodAsync(paymentMethodId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<SavedPaymentMethod>.Fail(info, StripeErrorMapper.ToError(result), result.RawBody);
        }

        return PaymentResponse<SavedPaymentMethod>.Ok(info, mappers.GetMapper<StripePaymentMethod, SavedPaymentMethod>().Map(result.Data)!, result.RawBody);
    }

    private PaymentResponse<PaymentMethodSetup> MapSetup(ApiResult<StripeSetupIntent> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            var error = StripeErrorMapper.Parse(result);
            var setup = error?.SetupIntent is { } intent ? mappers.GetMapper<StripeSetupIntent, PaymentMethodSetup>().Map(intent) : null;
            return PaymentResponse<PaymentMethodSetup>.Fail(info, StripeErrorMapper.ToError(error, result.StatusCode), setup, result.RawBody);
        }

        return PaymentResponse<PaymentMethodSetup>.Ok(info, mappers.GetMapper<StripeSetupIntent, PaymentMethodSetup>().Map(result.Data)!, result.RawBody);
    }
}
