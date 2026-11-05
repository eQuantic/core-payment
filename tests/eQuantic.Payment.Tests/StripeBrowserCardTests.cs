using System.Net;
using System.Security.Cryptography;
using System.Text;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// A card charge the customer's browser confirms, with Stripe.js and the Payment Element (a card, Apple Pay or Google
/// Pay): created with no card and unconfirmed, it comes back pending with the PaymentIntent's client secret, which is on
/// that result only, never on another charge nor in any raw body kept for auditing. It settles through the webhook.
/// </summary>
public class StripeBrowserCardTests
{
    private const string Secret = "pi_browser_secret_9f8e7d";
    private const string Redacted = "\"client_secret\":\"[redacted]\"";
    private const string WebhookSecret = "whsec_browser_card";

    private static readonly DateTimeOffset Now = new(2026, 11, 5, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 5);

    private static (StripeProviderV1 Provider, StubHttpMessageHandler Handler) CreateProvider(StripeApiVersion version = StripeApiVersion.V2025_04_30_Basil)
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        var provider = new StripeProviderV1(
            new StripeClientV1(http), version, TestMapperFactory.Create(), WebhookSecret, StripeDefaults.WebhookTolerance, new FixedClock(Now));
        return (provider, handler);
    }

    /// <summary>A card PaymentIntent in <paramref name="status"/>, holding its client secret, as Stripe returns every one.</summary>
    private static string Intent(string status, string lastError = "") => $$"""
    { "id": "pi_browser", "object": "payment_intent", "status": "{{status}}", "amount": 4990, "currency": "eur",
      "client_secret": "{{Secret}}", "capture_method": "automatic", "payment_method_types": ["card"],
      "metadata": { "reference_id": "order-44" }{{lastError}} }
    """;

    private const string Declined = """
    , "last_payment_error": { "type": "card_error", "code": "card_declined", "decline_code": "generic_decline", "message": "Your card was declined." }
    """;

    private static async Task<IReadOnlyList<KeyValuePair<string, string>>> FormAsync(CreateChargeRequest request, StripeApiVersion version = StripeApiVersion.V2025_04_30_Basil)
    {
        var (provider, handler) = CreateProvider(version);
        handler.EnqueueJson(HttpStatusCode.OK, Intent("requires_payment_method"));
        await provider.Charges.CreateAsync(request);
        return handler.Form(0);
    }

    [Fact]
    public async Task A_card_charge_with_no_card_is_created_unconfirmed_for_the_browser()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, Intent("requires_payment_method"));
        var request = new CreateChargeRequest
        {
            Amount = Money.Eur(49.90m),
            Method = PaymentMethodType.CreditCard,
            Customer = StripeIntents.Portuguese(),
            CustomerId = "cus_123",
            Description = "Order 44",
            ReferenceId = "order-44",
            IdempotencyKey = "order-44:card",
            Metadata = new Dictionary<string, string> { ["cart"] = "c-7" },
        };

        await provider.Charges.CreateAsync(request);

        Assert.Equal(
            [
                KeyValuePair.Create("amount", "4990"),
                KeyValuePair.Create("currency", "eur"),
                KeyValuePair.Create("payment_method_types[]", "card"),
                KeyValuePair.Create("customer", "cus_123"),
                KeyValuePair.Create("description", "Order 44"),
                KeyValuePair.Create("capture_method", "automatic"),
                KeyValuePair.Create("receipt_email", "joana.silva@example.com"),
                KeyValuePair.Create("metadata[reference_id]", "order-44"),
                KeyValuePair.Create("metadata[cart]", "c-7"),
                KeyValuePair.Create("expand[]", "latest_charge"),
            ],
            handler.Form(0));
        Assert.Equal("order-44:card", handler.Header(0, "Idempotency-Key"));
    }

    [Theory]
    [InlineData(PaymentMethodType.CreditCard)]
    [InlineData(PaymentMethodType.DebitCard)]
    public async Task Neither_a_confirmation_nor_a_payment_method_is_sent_for_the_browser(PaymentMethodType method)
    {
        var form = await FormAsync(new CreateChargeRequest { Amount = Money.Eur(49.90m), Method = method });

        Assert.Contains(KeyValuePair.Create("payment_method_types[]", "card"), form);
        Assert.DoesNotContain(form, field => field.Key is "confirm" or "payment_method" or "off_session" || field.Key.StartsWith("payment_method_data", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Card_details_with_no_card_to_charge_still_leave_it_to_the_browser()
    {
        var form = await FormAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(49.90m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { HolderName = "Joana Silva", Token = " ", PaymentMethodId = "" },
        });

        Assert.DoesNotContain(form, field => field.Key is "confirm" or "payment_method" || field.Key.StartsWith("payment_method_data", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_charge_only_authorized_in_the_browser_is_captured_manually()
    {
        var form = await FormAsync(new CreateChargeRequest { Amount = Money.Eur(49.90m), Method = PaymentMethodType.CreditCard, Capture = false });

        Assert.Contains(KeyValuePair.Create("capture_method", "manual"), form);
    }

    [Fact]
    public async Task Installments_are_offered_in_the_browser_with_no_plan_which_Stripe_takes_only_at_confirmation()
    {
        var form = await FormAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(300m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Installments = 3 },
        });

        Assert.Contains(KeyValuePair.Create("payment_method_options[card][installments][enabled]", "true"), form);
        Assert.DoesNotContain(form, field => field.Key.StartsWith("payment_method_options[card][installments][plan]", StringComparison.Ordinal));
    }

    [Fact]
    public async Task A_card_confirmed_here_still_sends_its_installment_plan()
    {
        var form = await FormAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(300m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "tok_visa", Installments = 3 },
        });

        Assert.Contains(KeyValuePair.Create("confirm", "true"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_options[card][installments][plan][count]", "3"), form);
    }

    [Fact]
    public async Task A_card_with_a_number_but_no_token_is_sent_confirmed_as_before()
    {
        var form = await FormAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(49.90m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Number = "4242424242424242", ExpirationMonth = 12, ExpirationYear = 2030, Cvv = "123" },
        });

        Assert.Contains(KeyValuePair.Create("confirm", "true"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_data[type]", "card"), form);
    }

    [Fact]
    public async Task With_the_customer_away_and_no_saved_card_the_charge_is_still_refused_before_Stripe()
    {
        var (provider, handler) = CreateProvider();

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(49.90m),
            Method = PaymentMethodType.CreditCard,
            CustomerId = "cus_123",
            OffSession = true,
        });

        Assert.False(response.Success);
        Assert.Equal("saved_payment_method_required", response.Error!.Code);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(StripeApiVersion.V2024_06_20, "payment_method_types[]")]
    [InlineData(StripeApiVersion.V2025_04_30_Basil, "payment_method_types[]")]
    [InlineData(StripeApiVersion.V2026_09_30_Endive, "allowed_payment_method_types[]")]
    public async Task At_every_version_the_browser_charge_names_card_in_the_versions_field_and_differs_in_nothing_else(StripeApiVersion version, string field)
    {
        var basil = await FormAsync(StripeIntents.BrowserCardCharge());
        var form = await FormAsync(StripeIntents.BrowserCardCharge(), version);

        Assert.Equal(["card"], form.Where(pair => pair.Key == field).Select(pair => pair.Value));
        Assert.DoesNotContain(form, pair => pair.Key != field && pair.Key.EndsWith("payment_method_types[]", StringComparison.Ordinal));
        Assert.Equal(basil.Select(pair => pair.Key == "payment_method_types[]" ? KeyValuePair.Create(field, pair.Value) : pair), form);
    }

    [Theory]
    [InlineData(StripeApiVersion.V2024_06_20)]
    [InlineData(StripeApiVersion.V2025_04_30_Basil)]
    [InlineData(StripeApiVersion.V2026_09_30_Endive)]
    public async Task The_charge_comes_back_pending_with_its_client_secret_for_the_browser(StripeApiVersion version)
    {
        var (provider, handler) = CreateProvider(version);
        handler.EnqueueJson(HttpStatusCode.OK, Intent("requires_payment_method"));

        var response = await provider.Charges.CreateAsync(StripeIntents.BrowserCardCharge());

        Assert.True(response.Success, response.Error?.Message);
        var charge = response.Data!;
        Assert.Equal("pi_browser", charge.Id);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(PaymentMethodType.CreditCard, charge.Method);
        Assert.Equal(Money.Eur(49.90m), charge.Amount);
        Assert.Equal("order-44", charge.ReferenceId);
        Assert.Equal(Secret, charge.ClientSecret);
    }

    [Fact]
    public async Task The_client_secret_reaches_the_result_and_never_the_raw_body_kept_for_auditing()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, Intent("requires_payment_method"));

        var response = await provider.Charges.CreateAsync(StripeIntents.BrowserCardCharge());

        Assert.Equal(Secret, response.Data!.ClientSecret);
        Assert.DoesNotContain(Secret, response.RawResponse);
        Assert.Contains(Redacted, response.RawResponse);
        Assert.Contains("\"id\":\"pi_browser\"", response.RawResponse);
        Assert.Contains("\"reference_id\":\"order-44\"", response.RawResponse);
    }

    [Fact]
    public async Task The_raw_body_keeps_the_rest_as_Stripe_wrote_it_accents_and_plus_signs_included()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "pi_boleto", "object": "payment_intent", "status": "requires_action", "amount": 9900, "currency": "brl",
          "client_secret": "pi_boleto_secret_1", "description": "Pedido nº 44 · São João & Cia <loja>",
          "payment_method_types": ["boleto"], "metadata": { "phone": "+5511987654321", "note": "it's paid" } }
        """);

        var response = await provider.Charges.CreateAsync(StripeIntents.Charge(StripeIntents.Boleto, Today));

        Assert.DoesNotContain("pi_boleto_secret_1", response.RawResponse);
        Assert.Contains("\"description\":\"Pedido nº 44 · São João & Cia <loja>\"", response.RawResponse);
        Assert.Contains("\"phone\":\"+5511987654321\"", response.RawResponse);
        Assert.Contains("\"note\":\"it's paid\"", response.RawResponse);
    }

    [Theory]
    [InlineData(StripeIntents.Card)]
    [InlineData(StripeIntents.SavedCard)]
    [InlineData(StripeIntents.Pix)]
    [InlineData(StripeIntents.Boleto)]
    [InlineData(StripeIntents.MbWay)]
    [InlineData(StripeIntents.Multibanco)]
    public async Task No_other_charge_created_carries_the_client_secret_nor_keeps_it_in_its_raw_body(string kind)
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, Intent("requires_action"));

        var response = await provider.Charges.CreateAsync(StripeIntents.Charge(kind, Today));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Null(response.Data!.ClientSecret);
        Assert.DoesNotContain(Secret, response.RawResponse);
        Assert.Contains(Redacted, response.RawResponse);
    }

    [Fact]
    public async Task A_charge_read_captured_or_canceled_carries_no_client_secret_nor_keeps_it_in_its_raw_body()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, Intent("requires_capture"))
            .EnqueueJson(HttpStatusCode.OK, Intent("succeeded"))
            .EnqueueJson(HttpStatusCode.OK, Intent("canceled"));

        var responses = new[]
        {
            await provider.Charges.GetAsync("pi_browser"),
            await provider.Charges.CaptureAsync("pi_browser"),
            await provider.Charges.CancelAsync("pi_browser"),
        };

        Assert.All(responses, response =>
        {
            Assert.True(response.Success, response.Error?.Message);
            Assert.Null(response.Data!.ClientSecret);
            Assert.DoesNotContain(Secret, response.RawResponse);
            Assert.Contains(Redacted, response.RawResponse);
        });
    }

    [Fact]
    public async Task A_declined_charge_carries_no_client_secret_nor_keeps_it_in_its_raw_body()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.PaymentRequired, $$"""
        { "error": { "type": "card_error", "code": "card_declined", "decline_code": "insufficient_funds",
          "message": "Your card has insufficient funds.", "payment_intent": {{Intent("requires_payment_method", Declined)}} } }
        """);

        var response = await provider.Charges.CreateAsync(StripeIntents.Charge(StripeIntents.Card, Today));

        Assert.False(response.Success);
        Assert.Equal("insufficient_funds", response.Error!.Code);
        Assert.Equal("pi_browser", response.Data!.Id);
        Assert.Null(response.Data.ClientSecret);
        Assert.DoesNotContain(Secret, response.RawResponse);
        Assert.Contains(Redacted, response.RawResponse);
    }

    [Theory]
    [InlineData("requires_payment_method", "", PaymentStatus.Pending)]
    [InlineData("requires_action", "", PaymentStatus.Pending)]
    [InlineData("processing", "", PaymentStatus.Processing)]
    [InlineData("requires_capture", "", PaymentStatus.Authorized)]
    [InlineData("succeeded", "", PaymentStatus.Paid)]
    [InlineData("canceled", "", PaymentStatus.Canceled)]
    [InlineData("requires_payment_method", Declined, PaymentStatus.Failed)]
    public async Task A_browser_confirmed_card_reads_as_the_unified_statuses(string status, string lastError, PaymentStatus expected)
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, Intent(status, lastError));

        var response = await provider.Charges.GetAsync("pi_browser");

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal(expected, response.Data!.Status);
        Assert.Equal(status, response.Data.ProviderStatus);
        Assert.Equal(PaymentMethodType.CreditCard, response.Data.Method);
    }

    private static string Event(string type, string status, string lastError = "") => $$"""
    { "id": "evt_browser", "object": "event", "api_version": "2025-04-30.basil", "created": {{Now.ToUnixTimeSeconds()}},
      "livemode": false, "type": "{{type}}", "data": { "object": {{Intent(status, lastError)}} } }
    """;

    private static NotificationRequest Signed(string body)
    {
        var timestamp = Now.ToUnixTimeSeconds();
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(WebhookSecret), Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return new NotificationRequest
        {
            Body = body,
            Headers = new Dictionary<string, string> { [StripeNotificationOperations.SignatureHeader] = $"t={timestamp},v1={signature}" },
        };
    }

    [Theory]
    [InlineData("payment_intent.succeeded", "succeeded", "", PaymentStatus.Paid)]
    [InlineData("payment_intent.payment_failed", "requires_payment_method", Declined, PaymentStatus.Failed)]
    [InlineData("payment_intent.amount_capturable_updated", "requires_capture", "", PaymentStatus.Authorized)]
    public void An_event_about_a_browser_confirmed_card_carries_its_charge_without_the_client_secret(string type, string status, string lastError, PaymentStatus expected)
    {
        var (provider, _) = CreateProvider();

        var response = provider.Notifications.Verify(Signed(Event(type, status, lastError)));

        Assert.True(response.Success, response.Error?.Message);
        var charge = response.Data!.Charge!;
        Assert.Equal("pi_browser", charge.Id);
        Assert.Equal(PaymentMethodType.CreditCard, charge.Method);
        Assert.Equal(expected, charge.Status);
        Assert.Equal("order-44", charge.ReferenceId);
        Assert.Null(charge.ClientSecret);
        Assert.DoesNotContain(Secret, response.RawResponse);
        Assert.Contains(Redacted, response.RawResponse);
        Assert.Contains("\"id\":\"evt_browser\"", response.RawResponse);
    }
}
