using System.Text.Json.Serialization;

namespace eQuantic.Payment.Stripe.V1.Models;

// Faithful wire models for the Stripe v1 REST API, mirroring the official reference 1:1
// (docs.stripe.com/api/payment_intents/object, charges/object, refunds/object, customers/object).
// JSON responses use snake_case keys; timestamps are Unix epoch seconds (integers).
// Note: since API version 2022-11-15 the PaymentIntent `charges` list was replaced by the single
// expandable `latest_charge` — modeled here accordingly (see StripeChargeExpandableConverter).

/// <summary>The <c>payment_intent</c> object.</summary>
public sealed class StripePaymentIntent
{
    /// <summary><c>pi_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Object { get; set; }
    public long Amount { get; set; }
    public long AmountCapturable { get; set; }
    public long AmountReceived { get; set; }
    public string? Currency { get; set; }

    /// <summary><c>automatic</c> | <c>automatic_async</c> | <c>manual</c>.</summary>
    public string? CaptureMethod { get; set; }

    /// <summary><c>automatic</c> | <c>manual</c>.</summary>
    public string? ConfirmationMethod { get; set; }

    public string? ClientSecret { get; set; }
    public long? Created { get; set; }

    /// <summary><c>cus_...</c> (expandable; kept as id).</summary>
    public string? Customer { get; set; }

    public string? Description { get; set; }

    /// <summary><c>in_...</c> (expandable; kept as id).</summary>
    public string? Invoice { get; set; }

    public StripeErrorDetail? LastPaymentError { get; set; }

    /// <summary>
    /// Single expandable charge (replaces the legacy <c>charges</c> list). Arrives as a <c>ch_...</c> id
    /// unless expanded via <c>expand[]=latest_charge</c>, in which case it's the full charge object.
    /// </summary>
    [JsonConverter(typeof(StripeChargeExpandableConverter))]
    public StripeCharge? LatestCharge { get; set; }

    public bool Livemode { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public StripeNextAction? NextAction { get; set; }

    /// <summary><c>pm_...</c> (expandable; kept as id).</summary>
    public string? PaymentMethod { get; set; }

    public StripePaymentMethodOptions? PaymentMethodOptions { get; set; }
    public List<string>? PaymentMethodTypes { get; set; }
    public string? ReceiptEmail { get; set; }

    /// <summary><c>on_session</c> | <c>off_session</c>.</summary>
    public string? SetupFutureUsage { get; set; }

    public string? StatementDescriptor { get; set; }
    public string? StatementDescriptorSuffix { get; set; }

    /// <summary><c>requires_payment_method</c> | <c>requires_confirmation</c> | <c>requires_action</c> | <c>processing</c> | <c>requires_capture</c> | <c>canceled</c> | <c>succeeded</c>.</summary>
    public string? Status { get; set; }

    public long? CanceledAt { get; set; }

    /// <summary><c>abandoned</c> | <c>automatic</c> | <c>duplicate</c> | <c>expired</c> | <c>failed_invoice</c> | <c>fraudulent</c> | <c>requested_by_customer</c> | <c>void_invoice</c>.</summary>
    public string? CancellationReason { get; set; }
}

public sealed class StripeNextAction
{
    /// <summary>Discriminator, e.g. <c>pix_display_qr_code</c>, <c>boleto_display_details</c>, <c>use_stripe_sdk</c>, <c>redirect_to_url</c>.</summary>
    public string? Type { get; set; }

    public StripePixDisplayQrCode? PixDisplayQrCode { get; set; }
    public StripeBoletoDisplayDetails? BoletoDisplayDetails { get; set; }
    public StripeRedirectToUrl? RedirectToUrl { get; set; }
}

public sealed class StripePixDisplayQrCode
{
    /// <summary>EMV "copia e cola" payload.</summary>
    public string? Data { get; set; }
    public long? ExpiresAt { get; set; }
    public string? HostedInstructionsUrl { get; set; }
    public string? ImageUrlPng { get; set; }
    public string? ImageUrlSvg { get; set; }
}

public sealed class StripeBoletoDisplayDetails
{
    public long? ExpiresAt { get; set; }
    public string? HostedVoucherUrl { get; set; }

    /// <summary>Boleto voucher number (linha digitável).</summary>
    public string? Number { get; set; }
    public string? Pdf { get; set; }
}

public sealed class StripeRedirectToUrl
{
    public string? ReturnUrl { get; set; }
    public string? Url { get; set; }
}

public sealed class StripePaymentMethodOptions
{
    public StripeCardOptions? Card { get; set; }
    public StripePixOptions? Pix { get; set; }
    public StripeBoletoOptions? Boleto { get; set; }
}

public sealed class StripeCardOptions
{
    public StripeInstallments? Installments { get; set; }
    public string? CaptureMethod { get; set; }
    public string? RequestThreeDSecure { get; set; }
}

public sealed class StripePixOptions
{
    public int? ExpiresAfterSeconds { get; set; }
    public long? ExpiresAt { get; set; }
}

public sealed class StripeBoletoOptions
{
    public int? ExpiresAfterDays { get; set; }
}

// ── Charge ────────────────────────────────────────────────────────────────────

/// <summary>The <c>charge</c> object (the expanded <c>latest_charge</c>).</summary>
public sealed class StripeCharge
{
    /// <summary><c>ch_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Object { get; set; }
    public long Amount { get; set; }
    public long AmountCaptured { get; set; }
    public long AmountRefunded { get; set; }
    public StripeBillingDetails? BillingDetails { get; set; }
    public string? CalculatedStatementDescriptor { get; set; }
    public bool Captured { get; set; }
    public long? Created { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public bool Disputed { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public StripeOutcome? Outcome { get; set; }
    public bool Paid { get; set; }

    /// <summary><c>pi_...</c> (expandable; kept as id).</summary>
    public string? PaymentIntent { get; set; }

    /// <summary><c>pm_...</c> / <c>card_...</c>.</summary>
    public string? PaymentMethod { get; set; }

    public StripePaymentMethodDetails? PaymentMethodDetails { get; set; }
    public string? ReceiptEmail { get; set; }
    public string? ReceiptNumber { get; set; }
    public string? ReceiptUrl { get; set; }
    public bool Refunded { get; set; }

    /// <summary><c>succeeded</c> | <c>pending</c> | <c>failed</c> (only three — distinct from PaymentIntent status).</summary>
    public string? Status { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class StripeBillingDetails
{
    public StripeAddress? Address { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? TaxId { get; set; }
}

public sealed class StripeAddress
{
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? PostalCode { get; set; }
    public string? State { get; set; }
}

public sealed class StripeOutcome
{
    public string? Type { get; set; }
    public string? NetworkStatus { get; set; }
    public string? RiskLevel { get; set; }
    public long? RiskScore { get; set; }
    public string? SellerMessage { get; set; }
    public string? Reason { get; set; }
}

/// <summary>The <c>payment_method_details</c> object; the populated sub-object matches <see cref="Type"/>.</summary>
public sealed class StripePaymentMethodDetails
{
    /// <summary>Discriminator, e.g. <c>card</c>, <c>pix</c>, <c>boleto</c>.</summary>
    public string? Type { get; set; }

    public StripeCardDetails? Card { get; set; }
    public StripePixDetails? Pix { get; set; }
    public StripeBoletoDetails? Boleto { get; set; }
}

public sealed class StripeCardDetails
{
    public long? AmountAuthorized { get; set; }
    public string? AuthorizationCode { get; set; }

    /// <summary><c>amex</c>, <c>mastercard</c>, <c>visa</c>, <c>elo</c>, <c>hipercard</c>, …</summary>
    public string? Brand { get; set; }

    public long? CaptureBefore { get; set; }
    public string? Country { get; set; }
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public string? Fingerprint { get; set; }

    /// <summary><c>credit</c> | <c>debit</c> | <c>prepaid</c> | <c>unknown</c>.</summary>
    public string? Funding { get; set; }

    public StripeInstallments? Installments { get; set; }
    public string? Last4 { get; set; }
    public string? Network { get; set; }
}

public sealed class StripeInstallments
{
    public StripeInstallmentPlan? Plan { get; set; }
}

public sealed class StripeInstallmentPlan
{
    public int? Count { get; set; }

    /// <summary><c>month</c>.</summary>
    public string? Interval { get; set; }

    /// <summary><c>fixed_count</c> | <c>bonus</c> | <c>revolving</c>.</summary>
    public string? Type { get; set; }
}

public sealed class StripePixDetails
{
    /// <summary>Brazilian Central Bank end-to-end id.</summary>
    public string? BankTransactionId { get; set; }
    public string? Fingerprint { get; set; }
}

public sealed class StripeBoletoDetails
{
    /// <summary>CPF (11 digits) or CNPJ (14 digits).</summary>
    public string? TaxId { get; set; }
}

// ── Refund ──────────────────────────────────────────────────────────────────

/// <summary>The <c>refund</c> object.</summary>
public sealed class StripeRefund
{
    /// <summary><c>re_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Object { get; set; }
    public long Amount { get; set; }

    /// <summary><c>ch_...</c> (expandable; kept as id).</summary>
    public string? Charge { get; set; }

    public long? Created { get; set; }
    public string? Currency { get; set; }
    public string? Description { get; set; }
    public string? FailureReason { get; set; }

    /// <summary><c>pi_...</c> (expandable; kept as id).</summary>
    public string? PaymentIntent { get; set; }

    /// <summary><c>duplicate</c> | <c>fraudulent</c> | <c>requested_by_customer</c> | <c>expired_uncaptured_charge</c>.</summary>
    public string? Reason { get; set; }

    public string? ReceiptNumber { get; set; }

    /// <summary><c>pending</c> | <c>requires_action</c> | <c>succeeded</c> | <c>failed</c> | <c>canceled</c>.</summary>
    public string? Status { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

// ── Customer ──────────────────────────────────────────────────────────────────

/// <summary>The <c>customer</c> object.</summary>
public sealed class StripeCustomer
{
    /// <summary><c>cus_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Object { get; set; }
    public StripeAddress? Address { get; set; }
    public long Balance { get; set; }
    public long? Created { get; set; }
    public string? Currency { get; set; }
    public bool? Delinquent { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public bool Livemode { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public StripeShipping? Shipping { get; set; }

    /// <summary><c>none</c> | <c>exempt</c> | <c>reverse</c>.</summary>
    public string? TaxExempt { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class StripeShipping
{
    public StripeAddress? Address { get; set; }
    public string? Name { get; set; }
    public string? Phone { get; set; }
}

// ── SetupIntent ───────────────────────────────────────────────────────────────

/// <summary>The <c>setup_intent</c> object: a payment method being saved for later charges.</summary>
public sealed class StripeSetupIntent
{
    /// <summary><c>seti_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Object { get; set; }
    public string? ClientSecret { get; set; }
    public long? Created { get; set; }

    /// <summary><c>cus_...</c> (expandable; kept as id).</summary>
    public string? Customer { get; set; }

    public string? Description { get; set; }
    public StripeErrorDetail? LastSetupError { get; set; }
    public bool Livemode { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public StripeNextAction? NextAction { get; set; }

    /// <summary><c>pm_...</c> (expandable; kept as id): the payment method saved once the setup succeeds.</summary>
    public string? PaymentMethod { get; set; }

    public List<string>? PaymentMethodTypes { get; set; }

    /// <summary><c>requires_payment_method</c> | <c>requires_confirmation</c> | <c>requires_action</c> | <c>processing</c> | <c>canceled</c> | <c>succeeded</c>.</summary>
    public string? Status { get; set; }

    /// <summary><c>on_session</c> | <c>off_session</c>.</summary>
    public string? Usage { get; set; }

    public string? CancellationReason { get; set; }
}

// ── PaymentMethod ─────────────────────────────────────────────────────────────

/// <summary>The <c>payment_method</c> object.</summary>
public sealed class StripePaymentMethod
{
    /// <summary><c>pm_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Object { get; set; }
    public StripeBillingDetails? BillingDetails { get; set; }
    public StripePaymentMethodCard? Card { get; set; }
    public long? Created { get; set; }

    /// <summary><c>cus_...</c> (expandable; kept as id); null once detached.</summary>
    public string? Customer { get; set; }

    public bool Livemode { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>Discriminator, e.g. <c>card</c>, <c>boleto</c>, <c>pix</c>.</summary>
    public string? Type { get; set; }
}

/// <summary>The <c>card</c> of a saved payment method.</summary>
public sealed class StripePaymentMethodCard
{
    /// <summary><c>amex</c>, <c>mastercard</c>, <c>visa</c>, <c>elo</c>, <c>hipercard</c>, …</summary>
    public string? Brand { get; set; }

    public string? Country { get; set; }
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public string? Fingerprint { get; set; }

    /// <summary><c>credit</c> | <c>debit</c> | <c>prepaid</c> | <c>unknown</c>.</summary>
    public string? Funding { get; set; }

    public string? Last4 { get; set; }
}

/// <summary>A page of a Stripe list (<c>{ "object": "list", "data": [...] }</c>).</summary>
public sealed class StripeList<T>
{
    public string? Object { get; set; }
    public List<T> Data { get; set; } = [];
    public bool HasMore { get; set; }
    public string? Url { get; set; }
}

// ── Error ─────────────────────────────────────────────────────────────────────

/// <summary>Error envelope <c>{ "error": { ... } }</c>.</summary>
public sealed class StripeErrorEnvelope
{
    public StripeErrorDetail? Error { get; set; }
}

public sealed class StripeErrorDetail
{
    /// <summary><c>api_error</c> | <c>card_error</c> | <c>idempotency_error</c> | <c>invalid_request_error</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Programmatic code, e.g. <c>card_declined</c>, <c>incorrect_cvc</c>, <c>expired_card</c>.</summary>
    public string? Code { get; set; }

    /// <summary>Issuer decline reason (card errors only).</summary>
    public string? DeclineCode { get; set; }

    public string? Message { get; set; }

    /// <summary>Offending parameter name.</summary>
    public string? Param { get; set; }

    public string? DocUrl { get; set; }

    /// <summary>How to proceed on a decline.</summary>
    public string? AdviceCode { get; set; }

    public string? NetworkAdviceCode { get; set; }
    public string? NetworkDeclineCode { get; set; }

    /// <summary>Failed charge id (card errors).</summary>
    public string? Charge { get; set; }

    /// <summary>The PaymentIntent the error is about, when it concerns one: a declined charge still has its id and status.</summary>
    public StripePaymentIntent? PaymentIntent { get; set; }

    /// <summary>The SetupIntent the error is about, when it concerns one.</summary>
    public StripeSetupIntent? SetupIntent { get; set; }
}
