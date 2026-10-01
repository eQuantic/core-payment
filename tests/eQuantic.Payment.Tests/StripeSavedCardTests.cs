using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme;
using eQuantic.Payment.Pagarme.V5;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// A card saved once through a SetupIntent, then charged by the engine on every cycle with the customer away. The
/// responses replay Stripe's documented objects and errors, the decline and authentication ones as Stripe's test
/// cards produce them.
/// </summary>
public class StripeSavedCardTests
{
    private const string Key = "attempt-42:create";

    private static (StripeProviderV1 Provider, StubHttpMessageHandler Handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        return (new StripeProviderV1(new StripeClientV1(http), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create()), handler);
    }

    private static CreateChargeRequest Renewal(string? paymentMethodId = "pm_123") => new()
    {
        Amount = Money.Brl(49.90m),
        Method = PaymentMethodType.CreditCard,
        CustomerId = "cus_123",
        Card = new CardDetails { PaymentMethodId = paymentMethodId },
        OffSession = true,
        IdempotencyKey = Key,
    };

    // A PaymentIntent whose off-session confirmation failed: back to requires_payment_method, with the error.
    private static string Declined(string code, string declineCode, string message) => $$"""
    {
      "error": {
        "type": "card_error",
        "code": "{{code}}",
        "decline_code": "{{declineCode}}",
        "message": "{{message}}",
        "payment_intent": {
          "id": "pi_declined",
          "object": "payment_intent",
          "amount": 4990,
          "currency": "brl",
          "status": "requires_payment_method",
          "payment_method_types": ["card"],
          "last_payment_error": { "type": "card_error", "code": "{{code}}", "decline_code": "{{declineCode}}" }
        }
      }
    }
    """;

    [Fact]
    public async Task A_customer_is_kept_in_step()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "cus_123", "object": "customer", "name": "Maria Souza", "email": "maria@example.com", "metadata": { "document": "12345678909" } }
        """);

        var response = await provider.Customers.UpdateAsync("cus_123", new CustomerRequest
        {
            Name = "Maria Souza",
            Email = "maria@example.com",
            Document = "123.456.789-09",
            Address = new AddressRequest { Line1 = "Av. Paulista, 1000", City = "São Paulo", State = "SP", ZipCode = "01310-000" },
        });

        Assert.True(response.Success);
        Assert.Equal("Maria Souza", response.Data!.Name);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("customers/cus_123", handler.RequestPaths[0]);
        var body = handler.LastRequestBody!;
        Assert.Contains("name=Maria+Souza", body);
        Assert.Contains("email=maria%40example.com", body);
        Assert.Contains("metadata%5Bdocument%5D=12345678909", body);
        Assert.Contains("address%5Bpostal_code%5D=01310-000", body);
    }

    [Fact]
    public async Task A_customer_update_clears_what_the_request_no_longer_carries()
    {
        var (provider, handler) = CreateProvider();
        const string Customer = """{ "id": "cus_123", "object": "customer", "name": "Maria Souza", "email": "maria@example.com" }""";
        handler.EnqueueJson(HttpStatusCode.OK, Customer);
        handler.EnqueueJson(HttpStatusCode.OK, Customer);

        await provider.Customers.UpdateAsync("cus_123", new CustomerRequest { Name = "Maria Souza", Email = "maria@example.com" });
        await provider.Customers.UpdateAsync("cus_123", new CustomerRequest
        {
            Name = "Maria Souza",
            Email = "maria@example.com",
            Phone = "+5511999999999",
            Document = "123.456.789-09",
            Address = new AddressRequest { Line1 = "Av. Paulista, 1000", City = "São Paulo", State = "SP", ZipCode = "01310-000" },
        });

        var cleared = handler.RequestBodies[0]!.Split('&');
        Assert.Contains("phone=", cleared);
        Assert.Contains("address=", cleared);
        Assert.Contains("metadata%5Bdocument%5D=", cleared);
        var replaced = handler.RequestBodies[1]!.Split('&');
        Assert.Contains("address%5Bline2%5D=", replaced);
        Assert.DoesNotContain("phone=", replaced);
        Assert.DoesNotContain("address=", replaced);
        Assert.DoesNotContain("metadata%5Bdocument%5D=", replaced);
    }

    [Fact]
    public async Task A_customer_is_created_under_the_callers_key_and_says_whose_it_is()
    {
        var (provider, handler) = CreateProvider();
        const string Customer = """
        { "id": "cus_123", "object": "customer", "name": "Maria Souza", "email": "maria@example.com",
          "metadata": { "billing_account_id": "account-7", "document": "12345678909" } }
        """;
        handler.EnqueueJson(HttpStatusCode.OK, Customer);
        handler.EnqueueJson(HttpStatusCode.OK, Customer);
        var request = new CustomerRequest
        {
            Name = "Maria Souza",
            Email = "maria@example.com",
            Document = "123.456.789-09",
            IdempotencyKey = "account-7:customer",
            Metadata = new Dictionary<string, string> { ["billing_account_id"] = "account-7" },
        };

        var created = await provider.Customers.CreateAsync(request);
        var updated = await provider.Customers.UpdateAsync("cus_123", request);

        Assert.True(created.Success);
        Assert.True(updated.Success);
        Assert.EndsWith("customers", handler.RequestPaths[0]);
        Assert.Equal("account-7:customer", handler.Header(0, "Idempotency-Key"));
        Assert.Null(handler.Header(1, "Idempotency-Key"));
        Assert.All(handler.RequestBodies, body =>
        {
            Assert.Contains("metadata%5Bbilling_account_id%5D=account-7", body);
            Assert.Contains("metadata%5Bdocument%5D=12345678909", body);
        });
    }

    [Fact]
    public async Task A_document_entry_of_the_callers_replaces_the_one_written_from_the_document()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """{ "id": "cus_123", "object": "customer", "name": "Maria Souza", "email": "maria@example.com" }""");

        await provider.Customers.CreateAsync(new CustomerRequest
        {
            Name = "Maria Souza",
            Email = "maria@example.com",
            Document = "123.456.789-09",
            Metadata = new Dictionary<string, string> { ["document"] = "CPF 123.456.789-09" },
        });

        var body = handler.LastRequestBody!;
        Assert.Contains("metadata%5Bdocument%5D=CPF+123.456.789-09", body);
        Assert.DoesNotContain("metadata%5Bdocument%5D=12345678909", body);
        Assert.Null(handler.Header(0, "Idempotency-Key"));
    }

    [Fact]
    public async Task A_card_is_saved_through_a_SetupIntent_whose_secret_the_front_end_confirms()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "seti_123", "object": "setup_intent", "client_secret": "seti_123_secret_abc", "customer": "cus_123",
          "status": "requires_payment_method", "usage": "off_session", "payment_method_types": ["card"], "created": 1790818200 }
        """);

        var response = await provider.PaymentMethods.SetupAsync(new PaymentMethodSetupRequest { CustomerId = "cus_123", IdempotencyKey = "account-7:setup" });

        Assert.True(response.Success);
        var setup = response.Data!;
        Assert.Equal("seti_123", setup.Id);
        Assert.Equal("seti_123_secret_abc", setup.ClientSecret);
        Assert.Equal(SetupStatus.Pending, setup.Status);
        Assert.Null(setup.PaymentMethodId);
        Assert.EndsWith("setup_intents", handler.RequestPaths[0]);
        Assert.Equal("account-7:setup", handler.Header(0, "Idempotency-Key"));
        var body = handler.LastRequestBody!;
        Assert.Contains("customer=cus_123", body);
        Assert.Contains("payment_method_types%5B%5D=card", body);
        Assert.Contains("usage=off_session", body);
    }

    [Fact]
    public async Task A_setups_client_secret_reaches_the_result_and_never_the_raw_body_kept_for_auditing()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "seti_123", "object": "setup_intent", "client_secret": "seti_123_secret_abc", "customer": "cus_123",
          "status": "requires_payment_method" }
        """);
        handler.EnqueueJson(HttpStatusCode.BadRequest, """
        { "error": { "type": "invalid_request_error", "code": "setup_intent_unexpected_state", "message": "This SetupIntent was canceled.",
          "setup_intent": { "id": "seti_123", "object": "setup_intent", "client_secret": "seti_123_secret_abc", "status": "canceled" } } }
        """);

        var created = await provider.PaymentMethods.SetupAsync(new PaymentMethodSetupRequest { CustomerId = "cus_123" });
        var refused = await provider.PaymentMethods.SetupAsync(new PaymentMethodSetupRequest { CustomerId = "cus_123" });

        Assert.Equal("seti_123_secret_abc", created.Data!.ClientSecret);
        Assert.DoesNotContain("seti_123_secret_abc", created.RawResponse);
        Assert.Contains("\"client_secret\":\"[redacted]\"", created.RawResponse);
        Assert.False(refused.Success);
        Assert.Equal("seti_123", refused.Data!.Id);
        Assert.DoesNotContain("seti_123_secret_abc", refused.RawResponse);
    }

    [Fact]
    public async Task A_setup_that_succeeded_names_the_card_it_saved()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "seti_123", "object": "setup_intent", "customer": "cus_123", "status": "succeeded", "payment_method": "pm_123" }
        """);

        var response = await provider.PaymentMethods.GetSetupAsync("seti_123");

        Assert.Equal(SetupStatus.Succeeded, response.Data!.Status);
        Assert.Equal("pm_123", response.Data.PaymentMethodId);
        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.EndsWith("setup_intents/seti_123", handler.RequestPaths[0]);
    }

    [Fact]
    public async Task The_customers_saved_cards_are_listed_and_one_is_detached()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "object": "list", "has_more": false, "data": [
          { "id": "pm_123", "object": "payment_method", "type": "card", "customer": "cus_123", "created": 1790818200,
            "card": { "brand": "visa", "last4": "4242", "exp_month": 12, "exp_year": 2030, "funding": "credit", "country": "US" } },
          { "id": "pm_456", "object": "payment_method", "type": "card", "customer": "cus_123",
            "card": { "brand": "mastercard", "last4": "4444", "exp_month": 1, "exp_year": 2031, "funding": "debit" } } ] }
        """);
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "pm_456", "object": "payment_method", "type": "card", "customer": null,
          "card": { "brand": "mastercard", "last4": "4444", "exp_month": 1, "exp_year": 2031, "funding": "debit" } }
        """);

        var listed = await provider.PaymentMethods.ListAsync("cus_123");
        var detached = await provider.PaymentMethods.DetachAsync("pm_456");

        Assert.Collection(listed.Data!,
            visa =>
            {
                Assert.Equal("pm_123", visa.Id);
                Assert.Equal("visa", visa.Brand);
                Assert.Equal("4242", visa.Last4);
                Assert.Equal(12, visa.ExpirationMonth);
                Assert.Equal(2030, visa.ExpirationYear);
                Assert.Equal(PaymentMethodType.CreditCard, visa.Method);
            },
            mastercard => Assert.Equal(PaymentMethodType.DebitCard, mastercard.Method));
        Assert.Contains("customers/cus_123/payment_methods?type=card", handler.RequestPaths[0]);

        Assert.True(detached.Success);
        Assert.Null(detached.Data!.CustomerId);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.EndsWith("payment_methods/pm_456/detach", handler.RequestPaths[1]);
    }

    [Fact]
    public async Task Every_saved_card_is_listed_page_after_page()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "object": "list", "has_more": true, "data": [
          { "id": "pm_1", "object": "payment_method", "type": "card", "customer": "cus_123", "card": { "brand": "visa", "last4": "4242" } } ] }
        """);
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "object": "list", "has_more": false, "data": [
          { "id": "pm_2", "object": "payment_method", "type": "card", "customer": "cus_123", "card": { "brand": "visa", "last4": "1881" } } ] }
        """);

        var listed = await provider.PaymentMethods.ListAsync("cus_123");

        Assert.True(listed.Success);
        Assert.Equal(new[] { "pm_1", "pm_2" }, listed.Data!.Select(card => card.Id));
        Assert.DoesNotContain("starting_after", handler.RequestPaths[0]);
        Assert.EndsWith("&starting_after=pm_1", handler.RequestPaths[1]);
        Assert.StartsWith("[{", listed.RawResponse);
    }

    [Fact]
    public async Task A_saved_card_is_charged_with_the_customer_away()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "pi_renewal", "status": "succeeded", "amount": 4990, "currency": "brl", "payment_method_types": ["card"],
          "customer": "cus_123", "payment_method": "pm_123" }
        """);

        var response = await provider.Charges.CreateAsync(Renewal());

        Assert.True(response.Success);
        Assert.Equal(PaymentStatus.Paid, response.Data!.Status);
        Assert.Equal(Key, handler.Header(0, "Idempotency-Key"));
        var body = handler.LastRequestBody!;
        Assert.Contains("customer=cus_123", body);
        Assert.Contains("payment_method=pm_123", body);
        Assert.Contains("off_session=true", body);
        Assert.Contains("confirm=true", body);
        Assert.DoesNotContain("payment_method_data", body);
    }

    [Fact]
    public async Task A_declined_card_is_an_outcome_with_its_decline_code_and_its_charge()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.PaymentRequired, Declined("card_declined", "insufficient_funds", "Your card has insufficient funds."));

        var response = await provider.Charges.CreateAsync(Renewal());

        Assert.False(response.Success);
        Assert.Equal("insufficient_funds", response.Error!.Code);
        Assert.Equal(402, response.Error.HttpStatusCode);
        Assert.Equal("pi_declined", response.Data!.Id);
        Assert.Equal(PaymentStatus.Failed, response.Data.Status);
        Assert.Equal("requires_payment_method", response.Data.ProviderStatus);
    }

    [Fact]
    public async Task A_card_that_asks_for_authentication_with_the_customer_away_is_an_outcome_too()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.PaymentRequired,
            Declined("authentication_required", "authentication_required", "Your card was declined. This transaction requires authentication."));

        var response = await provider.Charges.CreateAsync(Renewal());

        Assert.False(response.Success);
        Assert.Equal("authentication_required", response.Error!.Code);
        Assert.Equal("pi_declined", response.Data!.Id);
        Assert.Equal(PaymentStatus.Failed, response.Data.Status);
    }

    [Fact]
    public async Task A_charge_with_the_customer_away_needs_a_saved_card_and_its_customer()
    {
        var (provider, handler) = CreateProvider();

        var noCard = await provider.Charges.CreateAsync(Renewal(paymentMethodId: null));
        var blankCard = await provider.Charges.CreateAsync(Renewal(paymentMethodId: " "));
        var noCustomer = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(49.90m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { PaymentMethodId = "pm_123" },
            OffSession = true,
        });
        var blankCustomer = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(49.90m),
            Method = PaymentMethodType.CreditCard,
            CustomerId = " ",
            Card = new CardDetails { PaymentMethodId = "pm_123" },
            OffSession = true,
        });

        Assert.All([noCard, blankCard, noCustomer, blankCustomer], response => Assert.Equal("saved_payment_method_required", response.Error!.Code));
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task A_pm_id_passed_as_a_token_is_charged_as_the_saved_payment_method_1_x_documented()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """{ "id": "pi_1", "status": "succeeded", "amount": 4990, "currency": "brl", "payment_method_types": ["card"] }""");

        await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(49.90m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "pm_card_visa" },
        });

        Assert.Contains("payment_method=pm_card_visa", handler.LastRequestBody);
        Assert.DoesNotContain("payment_method_data", handler.LastRequestBody);
    }

    [Fact]
    public async Task A_provider_that_does_not_save_cards_or_update_customers_says_so()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(PagarmeDefaults.V5BaseUrl) };
        Abstractions.IPaymentProvider provider = new PagarmeProviderV5(new PagarmeClientV5(http), TestMapperFactory.Create());

        var setup = await provider.PaymentMethods.SetupAsync(new PaymentMethodSetupRequest { CustomerId = "cus_1" });
        var update = await provider.Customers.UpdateAsync("cus_1", new CustomerRequest { Name = "Maria", Email = "maria@example.com" });

        Assert.Equal("payment_methods_unsupported", setup.Error!.Code);
        Assert.Equal("customer_update_unsupported", update.Error!.Code);
        Assert.Empty(handler.Requests);
    }
}
