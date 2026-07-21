namespace eQuantic.Payment.Adyen.V71.Models;

/// <summary>
/// Adyen API error body returned on HTTP 4xx/5xx (a request rejected before processing), e.g.
/// <c>{ "status": 422, "errorCode": "101", "message": "Invalid card number", "errorType": "validation", "pspReference": "..." }</c>.
/// Distinct from a refusal, which is an HTTP 200 payment response with <c>resultCode: "Refused"</c>.
/// </summary>
public sealed class AdyenServiceError
{
    /// <summary>HTTP status code echoed in the body.</summary>
    public int Status { get; set; }

    /// <summary>Adyen error code, e.g. <c>"101"</c>.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Human-readable error message.</summary>
    public string? Message { get; set; }

    /// <summary>Error category: <c>validation</c>, <c>security</c>, <c>configuration</c> or <c>internal</c>.</summary>
    public string? ErrorType { get; set; }

    /// <summary>Adyen transaction reference, when available.</summary>
    public string? PspReference { get; set; }
}
