namespace eQuantic.Payment.Models;

/// <summary>Normalized error returned by any provider.</summary>
public sealed class PaymentError
{
    /// <summary>Provider-specific error code, when available.</summary>
    public string? Code { get; init; }

    public required string Message { get; init; }

    /// <summary>HTTP status code returned by the provider API, when the error came from an HTTP call.</summary>
    public int? HttpStatusCode { get; init; }
}
