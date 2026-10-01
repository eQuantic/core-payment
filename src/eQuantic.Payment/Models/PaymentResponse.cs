namespace eQuantic.Payment.Models;

/// <summary>
/// Unified response envelope: every operation, on every provider and every API version,
/// returns this single shape — provider identity, normalized data or error, and the raw payload.
/// </summary>
public sealed class PaymentResponse<TData>
{
    /// <summary>Which provider and API version produced this response (e.g. <c>pagarme@v5</c>).</summary>
    public required ProviderInfo Provider { get; init; }

    public required bool Success { get; init; }

    /// <summary>
    /// Normalized data when <see cref="Success"/> is <c>true</c>. On a failure, what the gateway still returned
    /// about the object, when it did: a declined charge, with its id and status.
    /// </summary>
    public TData? Data { get; init; }

    /// <summary>Normalized error when <see cref="Success"/> is <c>false</c>.</summary>
    public PaymentError? Error { get; init; }

    /// <summary>Raw provider response body, preserved for auditing/debugging.</summary>
    public string? RawResponse { get; init; }

    public static PaymentResponse<TData> Ok(ProviderInfo provider, TData data, string? rawResponse = null) => new()
    {
        Provider = provider,
        Success = true,
        Data = data,
        RawResponse = rawResponse,
    };

    public static PaymentResponse<TData> Fail(ProviderInfo provider, PaymentError error, string? rawResponse = null) => new()
    {
        Provider = provider,
        Success = false,
        Error = error,
        RawResponse = rawResponse,
    };

    /// <summary>A failure that still carries what the gateway returned about the object, such as a declined charge.</summary>
    public static PaymentResponse<TData> Fail(ProviderInfo provider, PaymentError error, TData? data, string? rawResponse) => new()
    {
        Provider = provider,
        Success = false,
        Error = error,
        Data = data,
        RawResponse = rawResponse,
    };
}
