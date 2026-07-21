namespace eQuantic.Payment.Adyen.V71.Models;

// Faithful wire models for the payment-modification endpoints (captures, cancels, refunds), mirroring the reference 1:1.
// All modifications are asynchronous: the synchronous response status is always "received"; the final
// outcome arrives via a webhook.

/// <summary>Request body for <c>POST /v71/payments/{pspReference}/captures</c>.</summary>
public sealed class AdyenCaptureRequest
{
    public string MerchantAccount { get; set; } = string.Empty;

    /// <summary>Amount to capture. Adyen requires it (supports partial/multiple captures).</summary>
    public AdyenAmount? Amount { get; set; }

    /// <summary>Your unique reference for this capture.</summary>
    public string? Reference { get; set; }
}

/// <summary>Request body for <c>POST /v71/payments/{pspReference}/cancels</c>.</summary>
public sealed class AdyenCancelRequest
{
    public string MerchantAccount { get; set; } = string.Empty;

    /// <summary>Your unique reference for this cancellation.</summary>
    public string? Reference { get; set; }
}

/// <summary>Request body for <c>POST /v71/payments/{pspReference}/refunds</c>.</summary>
public sealed class AdyenRefundRequest
{
    public string MerchantAccount { get; set; } = string.Empty;

    /// <summary>Amount to refund. Adyen requires it (supports partial refunds).</summary>
    public AdyenAmount? Amount { get; set; }

    /// <summary>Your unique reference for this refund.</summary>
    public string? Reference { get; set; }
}

/// <summary>
/// Shared response of the capture/cancel/refund endpoints. <see cref="Status"/> is always <c>"received"</c>
/// (asynchronous acknowledgement); the final outcome is delivered by a webhook.
/// </summary>
public sealed class AdyenModificationResponse
{
    public string? MerchantAccount { get; set; }

    /// <summary>The original payment's pspReference (the transaction being modified).</summary>
    public string? PaymentPspReference { get; set; }

    /// <summary>The modification's own pspReference.</summary>
    public string? PspReference { get; set; }

    /// <summary>Echo of the request reference.</summary>
    public string? Reference { get; set; }

    /// <summary>Always <c>"received"</c> (asynchronous acknowledgement).</summary>
    public string? Status { get; set; }

    /// <summary>Modification amount, when returned (captures/refunds).</summary>
    public AdyenAmount? Amount { get; set; }
}
