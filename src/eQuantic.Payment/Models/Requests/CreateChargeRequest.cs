namespace eQuantic.Payment.Models.Requests;

/// <summary>Provider-agnostic charge creation request.</summary>
public sealed class CreateChargeRequest
{
    public required Money Amount { get; init; }

    public required PaymentMethodType Method { get; init; }

    /// <summary>
    /// The payer. A boleto needs them whole on Stripe (name, email, CPF or CNPJ, address); Multibanco needs their email,
    /// where the gateway can send the voucher.
    /// </summary>
    public CustomerRequest? Customer { get; init; }

    /// <summary>
    /// The gateway's id of a customer created earlier (<c>Customers.CreateAsync</c>). Required to charge a payment
    /// method saved for that customer (<see cref="CardDetails.PaymentMethodId"/>).
    /// </summary>
    public string? CustomerId { get; init; }

    /// <summary>
    /// Charges with the customer away, as a renewal does, against a saved payment method. A card that would ask
    /// the customer to authenticate is declined instead (Stripe answers <c>authentication_required</c>), and the
    /// customer has to come back to pay.
    /// </summary>
    public bool OffSession { get; init; }

    /// <summary>Required when <see cref="Method"/> is credit/debit card.</summary>
    public CardDetails? Card { get; init; }

    /// <summary>Optional PIX settings; defaults apply when omitted.</summary>
    public PixDetails? Pix { get; init; }

    /// <summary>Optional boleto settings; defaults apply when omitted.</summary>
    public BoletoDetails? Boleto { get; init; }

    /// <summary>Required when <see cref="Method"/> is <see cref="PaymentMethodType.MbWay"/>: the customer's phone number.</summary>
    public MbWayDetails? MbWay { get; init; }

    public string? Description { get; init; }

    /// <summary>Your own correlation id (order id), forwarded to the provider when supported.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>When <c>false</c>, card charges are only authorized and must be captured later.</summary>
    public bool Capture { get; init; } = true;

    /// <summary>
    /// The key the gateway recognizes a retry of this request by, answering it with the first one's result
    /// instead of acting twice. One key per operation: derive it from your own id for the attempt and the
    /// operation (<c>attempt-42:create</c>, <c>attempt-42:refund</c>), and reuse it only to retry that same
    /// request, since a gateway refuses a key reused with other parameters: a retry sends the same request,
    /// <see cref="AttemptedAt"/> included. At most 64 characters. Gateways without idempotency keys ignore it, and
    /// those that need one send a fresh key when it is omitted.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// When this attempt was first made. What a gateway gets relative to the moment of the request (a Pix expiry
    /// from <see cref="PixDetails.ExpiresIn"/>, a boleto's default due date, Stripe's boleto days) counts from it,
    /// so a retry under the same <see cref="IdempotencyKey"/> sends what the first try sent. Set it with the key:
    /// left out, it is the time of each call, a retry counts from its own moment, and a gateway can refuse it for
    /// carrying other parameters under the same key.
    /// </summary>
    public DateTimeOffset? AttemptedAt { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>Card data (or a previously tokenized card).</summary>
public sealed class CardDetails
{
    /// <summary>
    /// Provider single-use card token (Stripe <c>tok_xxx</c>). When set, the raw card fields are ignored. A Stripe
    /// <c>pm_xxx</c> here is charged as <see cref="PaymentMethodId"/>, as 1.x documented.
    /// </summary>
    public string? Token { get; init; }

    /// <summary>
    /// A payment method saved for <see cref="CreateChargeRequest.CustomerId"/> (Stripe <c>pm_xxx</c>, from
    /// <c>PaymentMethods.SetupAsync</c>). When set, <see cref="Token"/> and the raw card fields are ignored.
    /// </summary>
    public string? PaymentMethodId { get; init; }

    public string? Number { get; init; }
    public string? HolderName { get; init; }
    public int ExpirationMonth { get; init; }
    public int ExpirationYear { get; init; }
    public string? Cvv { get; init; }

    /// <summary>
    /// Card brand/network (e.g. <c>visa</c>, <c>master</c>). Optional for providers that infer it from the
    /// token or PAN (Pagar.me, Stripe); required by Mercado Pago, which uses it as the <c>payment_method_id</c>.
    /// </summary>
    public string? Brand { get; init; }

    /// <summary>Number of installments (parcelas). Defaults to 1.</summary>
    public int Installments { get; init; } = 1;
}

public sealed class PixDetails
{
    /// <summary>How long the PIX QR code stays payable. Default: 1 hour.</summary>
    public TimeSpan ExpiresIn { get; init; } = TimeSpan.FromHours(1);
}

public sealed class BoletoDetails
{
    public DateOnly? DueDate { get; init; }
    public string? Instructions { get; init; }
}

/// <summary>What an MB WAY payment needs, on any gateway: the phone number the customer's MB WAY account is on.</summary>
public sealed class MbWayDetails
{
    /// <summary>Portugal's country calling code, given to a number typed without one.</summary>
    private const string PortugueseCallingCode = "351";

    /// <summary>The digits of a Portuguese number without its calling code.</summary>
    private const int PortugueseNumberLength = 9;

    /// <summary>
    /// The customer's phone number, as they typed it: <c>912 345 678</c>, <c>00351 912 345 678</c> and
    /// <c>+351 912-345-678</c> all reach the gateway as <c>+351912345678</c> (<see cref="InternationalPhoneNumber"/>).
    /// </summary>
    public required string PhoneNumber { get; init; }

    /// <summary>
    /// <see cref="PhoneNumber"/> as every gateway gets it, <c>+</c> and 7 to 15 digits (E.164), or null when it is not
    /// a phone number. Separators are dropped and a leading <c>00</c> is read as <c>+</c>; nine digits without a country
    /// code are a Portuguese number, where most MB WAY customers are, and a number from elsewhere keeps its own code.
    /// </summary>
    public string? InternationalPhoneNumber => PhoneNumbers.International(PhoneNumber, PortugueseCallingCode, PortugueseNumberLength);
}
