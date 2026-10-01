using System.Net;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Adyen;
using eQuantic.Payment.Adyen.V71;
using eQuantic.Payment.MercadoPago;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.MercadoPago.Orders;
using eQuantic.Payment.MercadoPago.Payments;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.PagSeguro;
using eQuantic.Payment.PagSeguro.Orders;
using eQuantic.Payment.Pagarme;
using eQuantic.Payment.Pagarme.V5;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// A retry after a timeout must not charge twice: every gateway that takes an idempotency key gets the caller's,
/// on create, capture, cancel and refund. The responses here are refusals, since only the request matters.
/// </summary>
public class IdempotencyKeyTests
{
    private const string Key = "attempt-0192f1a2-7c4e-7b8f-9a01-2b3c4d5e6f70";

    private static StubHttpMessageHandler Refusing(int calls)
    {
        var handler = new StubHttpMessageHandler();
        for (var i = 0; i < calls; i++)
        {
            handler.EnqueueJson(HttpStatusCode.BadRequest, "{}");
        }

        return handler;
    }

    private static HttpClient Http(StubHttpMessageHandler handler, string baseUrl) => new(handler) { BaseAddress = new Uri(baseUrl) };

    private static CreateChargeRequest Charge(string? key) => new()
    {
        Amount = Money.Brl(25.00m),
        Method = PaymentMethodType.Pix,
        Customer = new CustomerRequest { Name = "Maria", Email = "maria@example.com", Document = "12345678909" },
        IdempotencyKey = key,
    };

    /// <summary>Creates, captures, cancels and refunds once each, all under <paramref name="key"/>.</summary>
    private static async Task FourCallsAsync(IPaymentProvider provider, string chargeId, string? key)
    {
        await provider.Charges.CreateAsync(Charge(key));
        await provider.Charges.CaptureAsync(new CaptureRequest { ChargeId = chargeId, Amount = Money.Brl(25.00m), IdempotencyKey = key });
        await provider.Charges.CancelAsync(new CancelRequest { ChargeId = chargeId, IdempotencyKey = key });
        await provider.Refunds.CreateAsync(new RefundRequest { ChargeId = chargeId, Amount = Money.Brl(5.00m), IdempotencyKey = key });
    }

    [Fact]
    public async Task Stripe_sends_the_callers_key_and_none_without_one()
    {
        var handler = Refusing(8);
        var provider = new StripeProviderV1(new StripeClientV1(Http(handler, StripeDefaults.BaseUrl)), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create());

        await FourCallsAsync(provider, "pi_123", Key);
        await FourCallsAsync(provider, "pi_123", key: null);

        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(Key, handler.Header(i, "Idempotency-Key")));
        Assert.All(Enumerable.Range(4, 4), i => Assert.Null(handler.Header(i, "Idempotency-Key")));
    }

    [Fact]
    public async Task Mercado_Pago_payments_send_the_callers_key_on_every_call()
    {
        var handler = Refusing(8);
        var http = Http(handler, MercadoPagoDefaults.BaseUrl);
        var provider = new MercadoPagoPaymentsProvider(new MercadoPagoPaymentsClient(http), new MercadoPagoCustomerClient(http), TestMapperFactory.Create());

        await FourCallsAsync(provider, "1234567890", Key);
        await FourCallsAsync(provider, "1234567890", key: null);

        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(Key, handler.Header(i, "X-Idempotency-Key")));

        // Without one, a create and a refund still carry a fresh key, as the API requires; a capture and a
        // cancel update the payment in place and carry none.
        Assert.True(Guid.TryParse(handler.Header(4, "X-Idempotency-Key"), out _));
        Assert.Null(handler.Header(5, "X-Idempotency-Key"));
        Assert.Null(handler.Header(6, "X-Idempotency-Key"));
        Assert.True(Guid.TryParse(handler.Header(7, "X-Idempotency-Key"), out _));
    }

    [Fact]
    public async Task Mercado_Pago_orders_send_the_callers_key_on_every_call()
    {
        var handler = Refusing(4);
        var http = Http(handler, MercadoPagoDefaults.BaseUrl);
        var provider = new MercadoPagoOrdersProvider(new MercadoPagoOrdersClient(http), new MercadoPagoCustomerClient(http), TestMapperFactory.Create());

        await FourCallsAsync(provider, "ORD01JQ4S4KY8HWQ6NA5PXB65B3D3", Key);

        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(Key, handler.Header(i, "X-Idempotency-Key")));
    }

    [Fact]
    public async Task PagSeguro_sends_the_callers_key_or_a_fresh_one()
    {
        var handler = Refusing(8);
        var provider = new PagSeguroOrdersProvider(new PagSeguroOrdersClient(Http(handler, PagSeguroDefaults.BaseUrl)), TestMapperFactory.Create());

        await FourCallsAsync(provider, "CHAR_7A1B2C3D-0000-0000-0000-000000000000", Key);
        await FourCallsAsync(provider, "CHAR_7A1B2C3D-0000-0000-0000-000000000000", key: null);

        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(Key, handler.Header(i, "x-idempotency-key")));
        Assert.All(Enumerable.Range(4, 4), i => Assert.True(Guid.TryParse(handler.Header(i, "x-idempotency-key"), out _)));
    }

    [Fact]
    public async Task Adyen_sends_the_callers_key_or_a_fresh_one()
    {
        var handler = Refusing(8);
        var provider = new AdyenV71Provider(new AdyenClientV71(Http(handler, AdyenDefaults.BaseUrl)), "TestMerchant", AdyenApiVersion.V71, TestMapperFactory.Create());

        await FourCallsAsync(provider, "PSP0000000000001", Key);
        await FourCallsAsync(provider, "PSP0000000000001", key: null);

        Assert.All(Enumerable.Range(0, 4), i => Assert.Equal(Key, handler.Header(i, "Idempotency-Key")));
        Assert.All(Enumerable.Range(4, 4), i => Assert.True(Guid.TryParse(handler.Header(i, "Idempotency-Key"), out _)));
    }

    [Fact]
    public async Task Calls_written_for_1_x_compile_and_send_what_they_did()
    {
        // 1.x code passes the token positionally, `default` included. The overloads that take a key require their
        // token, so such a call can only be the 1.x overload's, and it sends what 1.x sent: no key to Stripe, a
        // fresh one where the gateway's API asks for one.
        var handler = Refusing(19);
        var stripe = new StripeClientV1(Http(handler, StripeDefaults.BaseUrl));
        await stripe.CreatePaymentIntentAsync([], default);
        await stripe.CapturePaymentIntentAsync("pi_1", null, default);
        await stripe.CancelPaymentIntentAsync("pi_1", default);
        await stripe.CreateRefundAsync([], default);

        var payments = new MercadoPagoPaymentsClient(Http(handler, MercadoPagoDefaults.BaseUrl));
        await payments.CreatePaymentAsync(new(), default);
        await payments.CapturePaymentAsync(1, new(), default);
        await payments.CancelPaymentAsync(1, default);
        await payments.CreateRefundAsync(1, new(), default);

        var orders = new MercadoPagoOrdersClient(Http(handler, MercadoPagoDefaults.BaseUrl));
        await orders.CreateOrderAsync(new(), default);
        await orders.CaptureOrderAsync("ORD1", default);
        await orders.CancelOrderAsync("ORD1", default);
        await orders.RefundOrderAsync("ORD1", new(), default);

        var pagSeguro = new PagSeguroOrdersClient(Http(handler, PagSeguroDefaults.BaseUrl));
        await pagSeguro.CreateOrderAsync(new(), default);
        await pagSeguro.CaptureChargeAsync("CHAR_1", null, default);
        await pagSeguro.CancelChargeAsync("CHAR_1", null, default);

        var adyen = new AdyenClientV71(Http(handler, AdyenDefaults.BaseUrl));
        await adyen.CreatePaymentAsync(new(), default);
        await adyen.CaptureAsync("PSP1", new(), default);
        await adyen.CancelAsync("PSP1", new(), default);
        await adyen.RefundAsync("PSP1", new(), default);

        Assert.Equal(19, handler.Requests.Count);
        Assert.All(Enumerable.Range(0, 4), i => Assert.Null(handler.Header(i, "Idempotency-Key")));
        Assert.True(Guid.TryParse(handler.Header(4, "X-Idempotency-Key"), out _));
        Assert.Null(handler.Header(5, "X-Idempotency-Key"));
        Assert.Null(handler.Header(6, "X-Idempotency-Key"));
        Assert.True(Guid.TryParse(handler.Header(7, "X-Idempotency-Key"), out _));
        Assert.All(Enumerable.Range(8, 4), i => Assert.True(Guid.TryParse(handler.Header(i, "X-Idempotency-Key"), out _)));
        Assert.All(Enumerable.Range(12, 3), i => Assert.True(Guid.TryParse(handler.Header(i, "x-idempotency-key"), out _)));
        Assert.All(Enumerable.Range(15, 4), i => Assert.True(Guid.TryParse(handler.Header(i, "Idempotency-Key"), out _)));
    }

    // Far from the wall clock, so a field computed from it instead could not pass for one computed from this.
    private static readonly DateTimeOffset AttemptedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static CreateChargeRequest Retried(PaymentMethodType method, DateOnly? dueDate = null) => new()
    {
        Amount = Money.Brl(25.00m),
        Method = method,
        Customer = new CustomerRequest
        {
            Name = "Maria",
            Email = "maria@example.com",
            Document = "12345678909",
            Address = new AddressRequest { Line1 = "Av. Paulista, 1000", City = "Sao Paulo", State = "SP", ZipCode = "01310-000" },
        },
        Boleto = dueDate is { } due ? new BoletoDetails { DueDate = due } : null,
        IdempotencyKey = Key,
        AttemptedAt = AttemptedAt,
    };

    /// <summary>Sends the same keyed request twice, as a retry does, and returns both bodies.</summary>
    private static async Task<(string First, string Second)> TwiceAsync(IPaymentProvider provider, StubHttpMessageHandler handler, CreateChargeRequest request)
    {
        await provider.Charges.CreateAsync(request);
        await Task.Delay(TimeSpan.FromMilliseconds(20));
        await provider.Charges.CreateAsync(request);
        return (handler.RequestBodies[^2]!, handler.RequestBodies[^1]!);
    }

    [Fact]
    public async Task A_retry_under_the_same_key_sends_the_same_request_on_every_gateway_that_takes_one()
    {
        var stripeHandler = Refusing(2);
        var stripe = new StripeProviderV1(new StripeClientV1(Http(stripeHandler, StripeDefaults.BaseUrl)), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create());
        var (stripeFirst, stripeSecond) = await TwiceAsync(stripe, stripeHandler, Retried(PaymentMethodType.Boleto, new DateOnly(2026, 1, 6)));
        Assert.Equal(stripeFirst, stripeSecond);
        // Midnight in UTC is still 31 December in São Paulo, where Stripe counts the voucher's days: six to 6 January.
        Assert.Contains("payment_method_options%5Bboleto%5D%5Bexpires_after_days%5D=6", stripeFirst);

        var paymentsHandler = Refusing(2);
        var paymentsHttp = Http(paymentsHandler, MercadoPagoDefaults.BaseUrl);
        var payments = new MercadoPagoPaymentsProvider(new MercadoPagoPaymentsClient(paymentsHttp), new MercadoPagoCustomerClient(paymentsHttp), TestMapperFactory.Create());
        var (paymentsFirst, paymentsSecond) = await TwiceAsync(payments, paymentsHandler, Retried(PaymentMethodType.Pix));
        Assert.Equal(paymentsFirst, paymentsSecond);
        Assert.Contains("2026-01-01T01:00:00", paymentsFirst);

        var ordersHandler = Refusing(2);
        var ordersHttp = Http(ordersHandler, MercadoPagoDefaults.BaseUrl);
        var orders = new MercadoPagoOrdersProvider(new MercadoPagoOrdersClient(ordersHttp), new MercadoPagoCustomerClient(ordersHttp), TestMapperFactory.Create());
        var (ordersFirst, ordersSecond) = await TwiceAsync(orders, ordersHandler, Retried(PaymentMethodType.Pix));
        Assert.Equal(ordersFirst, ordersSecond);
        Assert.Contains("2026-01-01T01:00:00", ordersFirst);

        var pagSeguroHandler = Refusing(4);
        var pagSeguro = new PagSeguroOrdersProvider(new PagSeguroOrdersClient(Http(pagSeguroHandler, PagSeguroDefaults.BaseUrl)), TestMapperFactory.Create());
        var (pixFirst, pixSecond) = await TwiceAsync(pagSeguro, pagSeguroHandler, Retried(PaymentMethodType.Pix));
        Assert.Equal(pixFirst, pixSecond);
        Assert.Contains("2026-01-01T01:00:00", pixFirst);
        var (boletoFirst, boletoSecond) = await TwiceAsync(pagSeguro, pagSeguroHandler, Retried(PaymentMethodType.Boleto));
        Assert.Equal(boletoFirst, boletoSecond);
        Assert.Contains("2026-01-04", boletoFirst);

        var adyenHandler = Refusing(2);
        var adyen = new AdyenV71Provider(new AdyenClientV71(Http(adyenHandler, AdyenDefaults.BaseUrl)), "TestMerchant", AdyenApiVersion.V71, TestMapperFactory.Create());
        var (adyenFirst, adyenSecond) = await TwiceAsync(adyen, adyenHandler, Retried(PaymentMethodType.Pix));
        Assert.Equal(adyenFirst, adyenSecond);
        Assert.Contains($"\"reference\":\"{Key}\"", adyenFirst);
        Assert.Contains("2026-01-01T01:00:00", adyenFirst);
    }

    [Fact]
    public async Task A_gateway_without_keys_still_captures_and_cancels_through_the_request()
    {
        var handler = Refusing(2);
        IPaymentProvider provider = new PagarmeProviderV5(new PagarmeClientV5(Http(handler, PagarmeDefaults.V5BaseUrl)), TestMapperFactory.Create());

        var captured = await provider.Charges.CaptureAsync(new CaptureRequest { ChargeId = "ch_123", Amount = Money.Brl(25.00m), IdempotencyKey = Key });
        var canceled = await provider.Charges.CancelAsync(new CancelRequest { ChargeId = "ch_123", IdempotencyKey = Key });

        Assert.False(captured.Success);
        Assert.False(canceled.Success);
        Assert.Contains("ch_123", handler.RequestPaths[0]);
        Assert.Contains("ch_123", handler.RequestPaths[1]);
    }
}
