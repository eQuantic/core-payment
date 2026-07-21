using System.Net.Http.Headers;
using eQuantic.Mapper;
using eQuantic.Payment.Adyen.V71;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Adyen;

public static class AdyenServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Adyen Checkout provider, so callers depend only on the unified <c>IPaymentProvider</c>.
    /// Authentication uses the <c>X-API-Key</c> header; the merchant account is sent as a body field on every request.
    /// </summary>
    public static PaymentBuilder AddAdyen(this PaymentBuilder builder, Action<AdyenOptions> configure, bool asDefault = false)
    {
        var options = new AdyenOptions { ApiKey = string.Empty, MerchantAccount = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("AdyenOptions.ApiKey is required.", nameof(configure));
        }

        if (string.IsNullOrWhiteSpace(options.MerchantAccount))
        {
            throw new ArgumentException("AdyenOptions.MerchantAccount is required.", nameof(configure));
        }

        // Register this assembly's eQuantic.Mapper mappers (idempotent for the factory itself).
        builder.Services.AddMappers(o => o.FromAssembly(typeof(AdyenServiceCollectionExtensions).Assembly));

        var baseUrl = options.BaseUrl ?? AdyenDefaults.BaseUrl;
        builder.Services.AddHttpClient(AdyenDefaults.HttpClientName, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("X-API-Key", options.ApiKey);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var info = new ProviderInfo(AdyenDefaults.ProviderName, AdyenDefaults.VersionString(options.Version));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var client = new AdyenClientV71(httpFactory.CreateClient(AdyenDefaults.HttpClientName));
            return new AdyenV71Provider(client, options.MerchantAccount, options.Version, mappers);
        }, asDefault);
    }
}
