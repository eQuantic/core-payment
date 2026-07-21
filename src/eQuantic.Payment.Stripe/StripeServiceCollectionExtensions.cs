using System.Net.Http.Headers;
using eQuantic.Mapper;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using eQuantic.Payment.Stripe.V1;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Stripe;

public static class StripeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Stripe provider. The pinned <c>Stripe-Version</c> is taken from
    /// <see cref="StripeOptions.Version"/> and surfaced in the unified <c>ProviderInfo</c>.
    /// </summary>
    public static PaymentBuilder AddStripe(this PaymentBuilder builder, Action<StripeOptions> configure, bool asDefault = false)
    {
        var options = new StripeOptions { ApiKey = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("StripeOptions.ApiKey is required.", nameof(configure));
        }

        // Register this assembly's eQuantic.Mapper mappers (idempotent for the factory itself).
        builder.Services.AddMappers(o => o.FromAssembly(typeof(StripeServiceCollectionExtensions).Assembly));

        var baseUrl = options.BaseUrl ?? StripeDefaults.BaseUrl;
        var stripeVersion = StripeDefaults.StripeVersionHeader(options.Version);

        builder.Services.AddHttpClient(StripeDefaults.HttpClientName, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            http.DefaultRequestHeaders.Add("Stripe-Version", stripeVersion);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var info = new ProviderInfo(StripeDefaults.ProviderName, StripeDefaults.VersionString(options.Version));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var client = new StripeClientV1(httpFactory.CreateClient(StripeDefaults.HttpClientName));
            return new StripeProviderV1(client, options.Version, mappers);
        }, asDefault);
    }
}
