using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>The answer of a provider whose gateway API, as this package speaks it, does not update customers.</summary>
public static class CustomerUpdates
{
    public static Task<PaymentResponse<Customer>> Unsupported(ProviderInfo info)
        => Task.FromResult(PaymentResponse<Customer>.Fail(info, new PaymentError
        {
            Code = "customer_update_unsupported",
            Message = $"Provider '{info.Key}' does not update customers yet; create a new one instead.",
        }));
}
