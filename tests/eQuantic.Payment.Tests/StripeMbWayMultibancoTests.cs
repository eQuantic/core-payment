using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// MB WAY and Multibanco, Portugal's two methods, through Stripe: in EUR only, within Stripe's limits; MB WAY with the
/// customer's phone, pending while they approve it in the app; Multibanco with the customer's email, pending with the
/// voucher they pay at an ATM. Both settle later, so their states come back through reads and notifications.
/// </summary>
public class StripeMbWayMultibancoTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 18, 0, 0, TimeSpan.Zero);

    private static (StripeProviderV1 Provider, StubHttpMessageHandler Handler) CreateProvider(StripeApiVersion version = StripeApiVersion.V2025_04_30_Basil)
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        var provider = new StripeProviderV1(
            new StripeClientV1(http), version, TestMapperFactory.Create(), null, StripeDefaults.WebhookTolerance, new FixedClock(Now));
        return (provider, handler);
    }

    private static CreateChargeRequest MbWay(Money amount, string? phone = "912 345 678") => new()
    {
        Amount = amount,
        Method = PaymentMethodType.MbWay,
        MbWay = phone is null ? null : new MbWayDetails { PhoneNumber = phone },
        ReferenceId = "order-42",
        IdempotencyKey = "order-42:mb-way",
    };

    private static CreateChargeRequest Multibanco(Money amount, CustomerRequest? customer = null) => new()
    {
        Amount = amount,
        Method = PaymentMethodType.Multibanco,
        Customer = customer ?? StripeIntents.Portuguese(),
        ReferenceId = "order-43",
    };

    private const string MbWayPendingJson = """
    { "id": "pi_mbway", "object": "payment_intent", "status": "requires_action", "amount": 2500, "currency": "eur",
      "payment_method_types": ["mb_way"], "metadata": { "reference_id": "order-42" } }
    """;

    // A voucher issued on 5 November, payable for seven days.
    private const string VoucherJson = """
    { "id": "pi_multibanco", "object": "payment_intent", "status": "requires_action", "amount": 9900, "currency": "eur",
      "payment_method_types": ["multibanco"], "metadata": { "reference_id": "order-43" },
      "next_action": { "type": "multibanco_display_details", "multibanco_display_details": {
        "entity": "12345", "reference": "123456789", "expires_at": 1794506400,
        "hosted_voucher_url": "https://payments.stripe.com/multibanco/voucher/test_123" } } }
    """;

    private const string PaidVoucherJson = """
    { "id": "pi_multibanco", "object": "payment_intent", "status": "succeeded", "amount": 9900, "currency": "eur",
      "payment_method_types": ["multibanco"], "next_action": null,
      "latest_charge": { "id": "ch_1", "object": "charge", "status": "succeeded", "amount": 9900, "currency": "eur",
        "payment_method_details": { "type": "multibanco", "multibanco": { "entity": "12345", "reference": "123456789" } } } }
    """;

    [Fact]
    public async Task An_MB_WAY_charge_sends_the_customers_phone_as_Stripe_takes_it()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, MbWayPendingJson);

        await provider.Charges.CreateAsync(MbWay(Money.Eur(25.00m)));

        var form = handler.Form(0);
        Assert.Contains(KeyValuePair.Create("amount", "2500"), form);
        Assert.Contains(KeyValuePair.Create("currency", "eur"), form);
        Assert.Contains(KeyValuePair.Create("confirm", "true"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_types[]", "mb_way"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_data[type]", "mb_way"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_data[billing_details][phone]", "+351912345678"), form);
        Assert.Contains(KeyValuePair.Create("metadata[reference_id]", "order-42"), form);
        Assert.DoesNotContain(form, field => field.Key == "capture_method");
        Assert.Equal("order-42:mb-way", handler.Header(0, "Idempotency-Key"));
    }

    [Theory]
    [InlineData("912345678", "+351912345678")]
    [InlineData("912 345 678", "+351912345678")]
    [InlineData(" 912.345.678 ", "+351912345678")]
    [InlineData("00351 912 345 678", "+351912345678")]
    [InlineData("00351-912-345-678", "+351912345678")]
    [InlineData("+351 912 345 678", "+351912345678")]
    [InlineData("+351 (912) 345-678", "+351912345678")]
    [InlineData("+44 7700 900123", "+447700900123")]
    public async Task An_MB_WAY_phone_is_written_as_plus_and_digits(string typed, string sent)
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, MbWayPendingJson);

        await provider.Charges.CreateAsync(MbWay(Money.Eur(25.00m), typed));

        Assert.Contains(KeyValuePair.Create("payment_method_data[billing_details][phone]", sent), handler.Form(0));
    }

    [Fact]
    public async Task An_MB_WAY_charge_waits_for_the_customer_to_approve_it_in_the_app()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, MbWayPendingJson);

        var response = await provider.Charges.CreateAsync(MbWay(Money.Eur(25.00m)));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal(PaymentMethodType.MbWay, response.Data!.Method);
        Assert.Equal(PaymentStatus.Pending, response.Data.Status);
        Assert.Equal(Money.Eur(25.00m), response.Data.Amount);
        Assert.Equal("order-42", response.Data.ReferenceId);
        Assert.Null(response.Data.Multibanco);
        Assert.Null(response.Data.Card);
    }

    [Fact]
    public async Task A_Multibanco_charge_sends_the_customers_email()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, VoucherJson);

        await provider.Charges.CreateAsync(Multibanco(Money.Eur(99.00m)));

        var form = handler.Form(0);
        Assert.Contains(KeyValuePair.Create("amount", "9900"), form);
        Assert.Contains(KeyValuePair.Create("currency", "eur"), form);
        Assert.Contains(KeyValuePair.Create("confirm", "true"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_types[]", "multibanco"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_data[type]", "multibanco"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_data[billing_details][email]", "joana.silva@example.com"), form);
        Assert.Contains(KeyValuePair.Create("payment_method_data[billing_details][name]", "Joana Silva"), form);
        Assert.DoesNotContain(form, field => field.Key == "capture_method");
    }

    [Fact]
    public async Task A_Multibanco_charge_comes_back_with_its_voucher()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, VoucherJson);

        var response = await provider.Charges.CreateAsync(Multibanco(Money.Eur(99.00m)));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal(PaymentMethodType.Multibanco, response.Data!.Method);
        Assert.Equal(PaymentStatus.Pending, response.Data.Status);
        Assert.Equal(Money.Eur(99.00m), response.Data.Amount);
        var voucher = response.Data.Multibanco!;
        Assert.Equal("12345", voucher.Entity);
        Assert.Equal("123456789", voucher.Reference);
        Assert.Equal(new DateTimeOffset(2026, 11, 12, 18, 0, 0, TimeSpan.Zero), voucher.ExpiresAt);
        Assert.Equal("https://payments.stripe.com/multibanco/voucher/test_123", voucher.HostedUrl);
    }

    [Fact]
    public async Task A_paid_Multibanco_charge_read_again_keeps_its_entity_and_reference()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, PaidVoucherJson);

        var response = await provider.Charges.GetAsync("pi_multibanco");

        Assert.Equal(PaymentStatus.Paid, response.Data!.Status);
        Assert.Equal("12345", response.Data.Multibanco!.Entity);
        Assert.Equal("123456789", response.Data.Multibanco.Reference);
        Assert.Null(response.Data.Multibanco.ExpiresAt);
        Assert.Null(response.Data.Multibanco.HostedUrl);
    }

    [Theory]
    [InlineData("BRL")]
    [InlineData("USD")]
    [InlineData("")]
    public async Task A_charge_in_another_currency_is_refused_before_the_call(string currency)
    {
        var (provider, handler) = CreateProvider();
        var amount = new Money(2500, currency);

        var mbWay = await provider.Charges.CreateAsync(MbWay(amount));
        var multibanco = await provider.Charges.CreateAsync(Multibanco(amount));

        Assert.All([mbWay, multibanco], response => Assert.Equal("currency_unsupported", response.Error!.Code));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task An_amount_left_at_the_BRL_default_never_reaches_Stripe()
    {
        var (provider, handler) = CreateProvider();

        var response = await provider.Charges.CreateAsync(MbWay(Money.FromDecimal(25.00m)));

        Assert.False(response.Success);
        Assert.Equal("currency_unsupported", response.Error!.Code);
        Assert.Contains("BRL", response.Error.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task The_currency_is_read_whatever_its_case()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, MbWayPendingJson);

        var response = await provider.Charges.CreateAsync(MbWay(new Money(2500, "eur")));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(PaymentMethodType.MbWay, 49)]
    [InlineData(PaymentMethodType.MbWay, 500_001)]
    [InlineData(PaymentMethodType.Multibanco, 49)]
    [InlineData(PaymentMethodType.Multibanco, 9_999_901)]
    [InlineData(PaymentMethodType.MbWay, 0)]
    [InlineData(PaymentMethodType.Multibanco, -100)]
    public async Task An_amount_outside_Stripes_limits_is_refused_before_the_call(PaymentMethodType method, long cents)
    {
        var (provider, handler) = CreateProvider();
        var amount = new Money(cents, "EUR");

        var response = await provider.Charges.CreateAsync(method == PaymentMethodType.MbWay ? MbWay(amount) : Multibanco(amount));

        Assert.False(response.Success);
        Assert.Equal("amount_out_of_range", response.Error!.Code);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData(PaymentMethodType.MbWay, 50)]
    [InlineData(PaymentMethodType.MbWay, 500_000)]
    [InlineData(PaymentMethodType.Multibanco, 50)]
    [InlineData(PaymentMethodType.Multibanco, 9_999_900)]
    public async Task An_amount_at_Stripes_limits_is_sent(PaymentMethodType method, long cents)
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, method == PaymentMethodType.MbWay ? MbWayPendingJson : VoucherJson);
        var amount = new Money(cents, "EUR");

        var response = await provider.Charges.CreateAsync(method == PaymentMethodType.MbWay ? MbWay(amount) : Multibanco(amount));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Contains(KeyValuePair.Create("amount", cents.ToString()), handler.Form(0));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("91234567")]
    [InlineData("351912345678")]
    [InlineData("+351 912 345 678 ext 2")]
    [InlineData("+0351912345678")]
    [InlineData("+1234567890123456")]
    [InlineData("phone")]
    public async Task An_MB_WAY_charge_without_a_phone_number_is_refused_before_the_call(string? phone)
    {
        var (provider, handler) = CreateProvider();

        var response = await provider.Charges.CreateAsync(MbWay(Money.Eur(25.00m), phone));

        Assert.False(response.Success);
        Assert.Equal("mb_way_phone_invalid", response.Error!.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task A_Multibanco_charge_without_the_customers_email_is_refused_before_the_call()
    {
        var (provider, handler) = CreateProvider();

        var noCustomer = await provider.Charges.CreateAsync(new CreateChargeRequest { Amount = Money.Eur(99.00m), Method = PaymentMethodType.Multibanco });
        var blankEmail = await provider.Charges.CreateAsync(Multibanco(Money.Eur(99.00m), new CustomerRequest { Name = "Joana Silva", Email = " " }));

        Assert.All([noCustomer, blankEmail], response => Assert.Equal("multibanco_email_required", response.Error!.Code));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("requires_action", null, PaymentStatus.Pending)]
    [InlineData("processing", null, PaymentStatus.Processing)]
    [InlineData("succeeded", null, PaymentStatus.Paid)]
    [InlineData("canceled", null, PaymentStatus.Canceled)]
    [InlineData("requires_payment_method", "payment_method_customer_decline", PaymentStatus.Failed)]
    [InlineData("requires_payment_method", "payment_method_provider_decline", PaymentStatus.Failed)]
    [InlineData("requires_payment_method", "payment_method_not_available", PaymentStatus.Failed)]
    [InlineData("requires_payment_method", "payment_intent_payment_attempt_expired", PaymentStatus.Expired)]
    public async Task An_MB_WAY_or_Multibanco_charge_read_again_says_where_it_stands(string status, string? lastError, PaymentStatus expected)
    {
        foreach (var (type, method) in new[] { ("mb_way", PaymentMethodType.MbWay), ("multibanco", PaymentMethodType.Multibanco) })
        {
            var (provider, handler) = CreateProvider();
            var error = lastError is null ? string.Empty : $$""", "last_payment_error": { "type": "invalid_request_error", "code": "{{lastError}}" }""";
            handler.EnqueueJson(HttpStatusCode.OK, $$"""
            { "id": "pi_1", "object": "payment_intent", "status": "{{status}}", "amount": 2500, "currency": "eur",
              "payment_method_types": ["{{type}}"]{{error}} }
            """);

            var response = await provider.Charges.GetAsync("pi_1");

            Assert.Equal(method, response.Data!.Method);
            Assert.Equal(expected, response.Data.Status);
            Assert.Equal(status, response.Data.ProviderStatus);
        }
    }

    [Theory]
    [InlineData(StripeApiVersion.V2024_06_20, "payment_method_types[]")]
    [InlineData(StripeApiVersion.V2025_04_30_Basil, "payment_method_types[]")]
    [InlineData(StripeApiVersion.V2026_09_30_Endive, "allowed_payment_method_types[]")]
    public async Task Each_version_names_the_method_in_its_own_field(StripeApiVersion version, string field)
    {
        foreach (var (request, type) in new[] { (MbWay(Money.Eur(25.00m)), "mb_way"), (Multibanco(Money.Eur(99.00m)), "multibanco") })
        {
            var (provider, handler) = CreateProvider(version);
            handler.EnqueueJson(HttpStatusCode.OK, MbWayPendingJson);

            await provider.Charges.CreateAsync(request);

            var form = handler.Form(0);
            Assert.Equal([type], form.Where(pair => pair.Key.EndsWith("payment_method_types[]", StringComparison.Ordinal)).Select(pair => pair.Value));
            Assert.Contains(form, pair => pair.Key == field);
        }
    }

    [Fact]
    public async Task An_MB_WAY_charge_with_the_customer_away_is_refused_as_any_off_session_charge_without_a_saved_card()
    {
        var (provider, handler) = CreateProvider();

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(25.00m),
            Method = PaymentMethodType.MbWay,
            MbWay = new MbWayDetails { PhoneNumber = "912345678" },
            CustomerId = "cus_123",
            OffSession = true,
        });

        Assert.Equal("saved_payment_method_required", response.Error!.Code);
        Assert.Empty(handler.Requests);
    }
}
