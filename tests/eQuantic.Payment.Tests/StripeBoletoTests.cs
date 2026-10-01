using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// A boleto is an invoice the customer pays: the engine asks for one per invoice, hands the customer its voucher,
/// and learns later from Stripe whether it was paid or expired. Stripe counts a voucher's days in São Paulo.
/// </summary>
public class StripeBoletoTests
{
    // 22:30 on 30 September in São Paulo, already 1 October in UTC.
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 22, 30, 0, TimeSpan.FromHours(-3));

    private static (StripeProviderV1 Provider, StubHttpMessageHandler Handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        var provider = new StripeProviderV1(
            new StripeClientV1(http), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create(), null, StripeDefaults.WebhookTolerance, new FixedClock(Now));
        return (provider, handler);
    }

    private static CreateChargeRequest Boleto(CustomerRequest? payer) => new()
    {
        Amount = Money.Brl(99.00m),
        Method = PaymentMethodType.Boleto,
        Customer = payer,
        Boleto = new BoletoDetails { DueDate = new DateOnly(2026, 10, 3) },
        IdempotencyKey = "invoice-2026-000042:boleto",
    };

    private static CustomerRequest Payer() => new()
    {
        Name = "João da Conceição",
        Email = "joao@example.com",
        Document = "123.456.789-09",
        Address = new AddressRequest { Line1 = "Rua Álvares Cabral, nº 10", Line2 = "Apto 2º", City = "São Paulo", State = "SP", ZipCode = "01310-000" },
    };

    // The voucher of a boleto created that evening to be paid by 3 October: it expires at 23:59:59 in São Paulo.
    private const string VoucherJson = """
    { "id": "pi_boleto", "status": "requires_action", "amount": 9900, "currency": "brl", "payment_method_types": ["boleto"],
      "next_action": { "type": "boleto_display_details", "boleto_display_details": {
        "number": "34191.79001 01043.510047 91020.150008 1 96120000009900",
        "pdf": "https://payments.stripe.com/boleto/voucher/test_123/pdf",
        "hosted_voucher_url": "https://payments.stripe.com/boleto/voucher/test_123",
        "expires_at": 1791082799 } } }
    """;

    [Fact]
    public async Task A_boleto_carries_its_payer_in_ascii_and_counts_its_days_in_sao_paulo()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, VoucherJson);

        await provider.Charges.CreateAsync(Boleto(Payer()));

        var body = Uri.UnescapeDataString(handler.LastRequestBody!.Replace('+', ' '));
        Assert.Contains("payment_method_options[boleto][expires_after_days]=3", body);
        Assert.Contains("payment_method_data[type]=boleto", body);
        Assert.Contains("payment_method_data[boleto][tax_id]=12345678909", body);
        Assert.Contains("payment_method_data[billing_details][name]=Joao da Conceicao", body);
        Assert.Contains("payment_method_data[billing_details][email]=joao@example.com", body);
        Assert.Contains("payment_method_data[billing_details][address][line1]=Rua Alvares Cabral, no 10", body);
        Assert.Contains("payment_method_data[billing_details][address][line2]=Apto 2o", body);
        Assert.Contains("payment_method_data[billing_details][address][city]=Sao Paulo", body);
        Assert.Contains("payment_method_data[billing_details][address][postal_code]=01310-000", body);
        Assert.Equal("invoice-2026-000042:boleto", handler.Header(0, "Idempotency-Key"));
    }

    [Fact]
    public async Task A_boleto_comes_back_with_its_number_pdf_hosted_page_and_expiry()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, VoucherJson);

        var response = await provider.Charges.CreateAsync(Boleto(Payer()));

        Assert.True(response.Success);
        Assert.Equal(PaymentStatus.Pending, response.Data!.Status);
        var voucher = response.Data.Boleto!;
        Assert.Equal("34191.79001 01043.510047 91020.150008 1 96120000009900", voucher.DigitableLine);
        Assert.Equal("https://payments.stripe.com/boleto/voucher/test_123/pdf", voucher.PdfUrl);
        Assert.Equal("https://payments.stripe.com/boleto/voucher/test_123", voucher.HostedUrl);
        Assert.Equal(voucher.PdfUrl, voucher.Url);
        Assert.Equal(new DateTimeOffset(2026, 10, 3, 23, 59, 59, TimeSpan.FromHours(-3)), voucher.ExpiresAt);
        Assert.Equal(new DateOnly(2026, 10, 3), voucher.DueDate);
    }

    [Fact]
    public async Task A_boleto_without_its_whole_payer_is_refused_before_the_call()
    {
        var (provider, handler) = CreateProvider();
        var payer = Payer();

        var noPayer = await provider.Charges.CreateAsync(Boleto(null));
        var noDocument = await provider.Charges.CreateAsync(Boleto(new CustomerRequest { Name = payer.Name, Email = payer.Email, Address = payer.Address }));
        var noAddress = await provider.Charges.CreateAsync(Boleto(new CustomerRequest { Name = payer.Name, Email = payer.Email, Document = payer.Document }));
        var blankName = await provider.Charges.CreateAsync(Boleto(new CustomerRequest
        {
            Name = "  ", Email = payer.Email, Document = payer.Document, Address = payer.Address,
        }));
        var noCountry = await provider.Charges.CreateAsync(Boleto(new CustomerRequest
        {
            Name = payer.Name,
            Email = payer.Email,
            Document = payer.Document,
            Address = new AddressRequest { Line1 = "Rua Álvares Cabral, nº 10", City = "São Paulo", State = "SP", ZipCode = "01310-000", Country = " " },
        }));

        Assert.All([noPayer, noDocument, noAddress, blankName, noCountry], response => Assert.Equal("boleto_payer_incomplete", response.Error!.Code));
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("""{ "id": "pi_1", "status": "succeeded", "amount": 9900, "currency": "brl", "payment_method_types": ["boleto"] }""", PaymentStatus.Paid)]
    [InlineData("""{ "id": "pi_1", "status": "requires_payment_method", "amount": 9900, "currency": "brl", "payment_method_types": ["boleto"], "last_payment_error": { "type": "invalid_request_error", "code": "payment_intent_payment_attempt_expired" } }""", PaymentStatus.Expired)]
    [InlineData("""{ "id": "pi_1", "status": "requires_payment_method", "amount": 4990, "currency": "brl", "payment_method_types": ["card"], "last_payment_error": { "type": "card_error", "code": "card_declined", "decline_code": "generic_decline" } }""", PaymentStatus.Failed)]
    [InlineData("""{ "id": "pi_1", "status": "requires_payment_method", "amount": 4990, "currency": "brl", "payment_method_types": ["card"] }""", PaymentStatus.Pending)]
    public async Task A_payment_intent_read_again_says_whether_it_was_paid_expired_or_failed(string json, PaymentStatus expected)
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, json);

        var response = await provider.Charges.GetAsync("pi_1");

        Assert.Equal(expected, response.Data!.Status);
    }
}
