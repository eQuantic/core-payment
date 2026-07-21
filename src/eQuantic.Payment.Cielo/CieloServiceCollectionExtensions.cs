using eQuantic.Mapper;
using eQuantic.Payment.Cielo.V3;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Cielo;

/// <summary>Registration entry point for the Cielo provider.</summary>
public static class CieloServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Cielo provider (E-commerce API 3.0), so callers depend only on the unified
    /// <c>IPaymentProvider</c>. Authentication uses the dual <c>MerchantId</c> + <c>MerchantKey</c> headers.
    /// </summary>
    public static PaymentBuilder AddCielo(this PaymentBuilder builder, Action<CieloOptions> configure, bool asDefault = false)
    {
        var options = new CieloOptions { MerchantId = string.Empty, MerchantKey = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.MerchantId))
        {
            throw new ArgumentException("CieloOptions.MerchantId is required.", nameof(configure));
        }

        if (string.IsNullOrWhiteSpace(options.MerchantKey))
        {
            throw new ArgumentException("CieloOptions.MerchantKey is required.", nameof(configure));
        }

        // Register this assembly's eQuantic.Mapper mappers (idempotent for the factory itself).
        builder.Services.AddMappers(o => o.FromAssembly(typeof(CieloServiceCollectionExtensions).Assembly));

        var baseUrl = options.BaseUrl ?? CieloDefaults.BaseUrl;
        var queryBaseUrl = options.QueryBaseUrl ?? CieloDefaults.QueryBaseUrl;

        builder.Services.AddHttpClient(CieloDefaults.V3HttpClientName, http =>
        {
            // Transactional host (create/capture/void); GET consults use the query host as an absolute URI.
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Add("MerchantId", options.MerchantId);
            http.DefaultRequestHeaders.Add("MerchantKey", options.MerchantKey);
            http.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        var info = new ProviderInfo(CieloDefaults.ProviderName, CieloDefaults.VersionString(CieloApiVersion.V3));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var client = new CieloClient(httpFactory.CreateClient(CieloDefaults.V3HttpClientName), queryBaseUrl);
            return new CieloV3Provider(client, mappers);
        }, asDefault);
    }
}
