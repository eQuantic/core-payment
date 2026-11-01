using System.Net;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Tests;

/// <summary>
/// Stripe's API version 2026-09-30.endive, where a PaymentIntent or a SetupIntent names the payment method types it
/// accepts in <c>allowed_payment_method_types</c> and is refused for <c>payment_method_types</c>; the versions before
/// it, which do not know the new field, keep the old one. Everything else in the request is the same at every version.
/// </summary>
public class StripeEndiveTests
{
    private const string Allowed = "allowed_payment_method_types[]";
    private const string Legacy = "payment_method_types[]";

    // 22:30 on 30 September in São Paulo, already 1 October in UTC.
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 22, 30, 0, TimeSpan.FromHours(-3));
    private static readonly DateOnly Today = new(2026, 9, 30);

    private const string IntentJson = """{ "id": "pi_123", "status": "requires_action", "amount": 2500, "currency": "brl" }""";

    private const string SetupJson = """
    { "id": "seti_123", "object": "setup_intent", "client_secret": "seti_123_secret_abc", "customer": "cus_123",
      "status": "requires_payment_method", "usage": "off_session", "payment_method_types": ["card"], "created": 1790818200 }
    """;

    private static (StripeProviderV1 Provider, StubHttpMessageHandler Handler) CreateProvider(StripeApiVersion version)
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        var provider = new StripeProviderV1(
            new StripeClientV1(http), version, TestMapperFactory.Create(), null, StripeDefaults.WebhookTolerance, new FixedClock(Now));
        return (provider, handler);
    }

    /// <summary>Stripe registered the way a consumer registers it, its HTTP answered by <paramref name="handler"/>.</summary>
    private static IPaymentProviderFactory Register(StubHttpMessageHandler handler, Action<StripeOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddPayments(payments => payments.AddStripe(options =>
        {
            options.ApiKey = "sk_test_123";
            configure(options);
        }));
        services.AddHttpClient(StripeDefaults.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider().GetRequiredService<IPaymentProviderFactory>();
    }

    private static async Task<IReadOnlyList<KeyValuePair<string, string>>> ChargeFormAsync(StripeApiVersion version, string kind)
    {
        var (provider, handler) = CreateProvider(version);
        handler.EnqueueJson(HttpStatusCode.OK, IntentJson);
        await provider.Charges.CreateAsync(StripeIntents.Charge(kind, Today));
        return handler.Form(0);
    }

    private static async Task<IReadOnlyList<KeyValuePair<string, string>>> SetupFormAsync(StripeApiVersion version)
    {
        var (provider, handler) = CreateProvider(version);
        handler.EnqueueJson(HttpStatusCode.OK, SetupJson);
        await provider.PaymentMethods.SetupAsync(StripeIntents.CardSetup());
        return handler.Form(0);
    }

    [Fact]
    public async Task A_consumer_pinning_Endive_sends_its_header_and_is_answered_in_its_name()
    {
        var handler = new StubHttpMessageHandler().EnqueueJson(HttpStatusCode.OK, IntentJson);
        var stripe = Register(handler, options => options.Version = StripeApiVersion.V2026_09_30_Endive).Get("stripe");

        var response = await stripe.Charges.CreateAsync(StripeIntents.Charge(StripeIntents.Pix, Today));

        Assert.Equal("2026-09-30.endive", handler.Header(0, "Stripe-Version"));
        Assert.Equal("stripe@2026-09-30.endive", stripe.Info.Key);
        Assert.Equal("2026-09-30.endive", response.Provider.Version);
    }

    [Fact]
    public async Task A_consumer_naming_no_version_pins_Basil()
    {
        var handler = new StubHttpMessageHandler().EnqueueJson(HttpStatusCode.OK, IntentJson);
        var stripe = Register(handler, _ => { }).Get("stripe");

        var response = await stripe.Charges.CreateAsync(StripeIntents.Charge(StripeIntents.Pix, Today));

        Assert.Equal("2025-04-30.basil", handler.Header(0, "Stripe-Version"));
        Assert.Equal("2025-04-30.basil", response.Provider.Version);
    }

    [Fact]
    public void Endive_resolves_beside_the_versions_before_it()
    {
        var services = new ServiceCollection();
        services.AddPayments(payments => payments
            .AddStripe(options => { options.ApiKey = "sk_a"; options.Version = StripeApiVersion.V2024_06_20; })
            .AddStripe(options => { options.ApiKey = "sk_b"; options.Version = StripeApiVersion.V2025_04_30_Basil; })
            .AddStripe(options => { options.ApiKey = "sk_c"; options.Version = StripeApiVersion.V2026_09_30_Endive; }));
        var factory = services.BuildServiceProvider().GetRequiredService<IPaymentProviderFactory>();

        Assert.Equal("stripe@2024-06-20", factory.Get("stripe", "2024-06-20").Info.Key);
        Assert.Equal("stripe@2025-04-30.basil", factory.Get("stripe", "2025-04-30.basil").Info.Key);
        Assert.Equal("stripe@2026-09-30.endive", factory.Get("stripe", "2026-09-30.endive").Info.Key);
    }

    [Theory]
    [InlineData(StripeIntents.Card, "card")]
    [InlineData(StripeIntents.SavedCard, "card")]
    [InlineData(StripeIntents.Pix, "pix")]
    [InlineData(StripeIntents.Boleto, "boleto")]
    public async Task At_Endive_a_charge_names_its_type_in_allowed_payment_method_types(string kind, string type)
    {
        var form = await ChargeFormAsync(StripeApiVersion.V2026_09_30_Endive, kind);

        Assert.Equal([type], form.Where(field => field.Key == Allowed).Select(field => field.Value));
        Assert.DoesNotContain(form, field => field.Key == Legacy);
    }

    [Fact]
    public async Task At_Endive_a_card_setup_names_card_in_allowed_payment_method_types()
    {
        var form = await SetupFormAsync(StripeApiVersion.V2026_09_30_Endive);

        Assert.Equal(["card"], form.Where(field => field.Key == Allowed).Select(field => field.Value));
        Assert.DoesNotContain(form, field => field.Key == Legacy);
    }

    [Theory]
    [InlineData(StripeApiVersion.V2024_06_20)]
    [InlineData(StripeApiVersion.V2025_04_30_Basil)]
    public async Task Before_Endive_a_charge_and_a_setup_keep_payment_method_types(StripeApiVersion version)
    {
        foreach (var form in new[] { await ChargeFormAsync(version, StripeIntents.Card), await SetupFormAsync(version) })
        {
            Assert.Equal(["card"], form.Where(field => field.Key == Legacy).Select(field => field.Value));
            Assert.DoesNotContain(form, field => field.Key == Allowed);
        }
    }

    [Theory]
    [InlineData(StripeIntents.Card)]
    [InlineData(StripeIntents.SavedCard)]
    [InlineData(StripeIntents.Pix)]
    [InlineData(StripeIntents.Boleto)]
    public async Task At_Endive_a_charge_differs_from_Basil_only_in_the_name_of_that_field(string kind)
    {
        var basil = await ChargeFormAsync(StripeApiVersion.V2025_04_30_Basil, kind);
        var endive = await ChargeFormAsync(StripeApiVersion.V2026_09_30_Endive, kind);

        Assert.Equal(basil.Select(field => field.Key == Legacy ? KeyValuePair.Create(Allowed, field.Value) : field), endive);
    }

    [Fact]
    public async Task At_Endive_a_card_setup_differs_from_Basil_only_in_the_name_of_that_field()
    {
        var basil = await SetupFormAsync(StripeApiVersion.V2025_04_30_Basil);
        var endive = await SetupFormAsync(StripeApiVersion.V2026_09_30_Endive);

        Assert.Equal(basil.Select(field => field.Key == Legacy ? KeyValuePair.Create(Allowed, field.Value) : field), endive);
    }
}
