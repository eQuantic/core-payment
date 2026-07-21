using System.Net.Http.Headers;
using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Asaas;

public static class AsaasServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Asaas provider (API v3), so callers depend only on the unified <c>IPaymentProvider</c>.
    /// Authentication uses the custom <c>access_token</c> header (not <c>Bearer</c>) plus a mandatory
    /// <c>User-Agent</c> header.
    /// </summary>
    public static PaymentBuilder AddAsaas(this PaymentBuilder builder, Action<AsaasOptions> configure, bool asDefault = false)
    {
        var options = new AsaasOptions { ApiKey = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("AsaasOptions.ApiKey is required.", nameof(configure));
        }

        if (options.Version != AsaasApiVersion.V3)
        {
            throw new ArgumentOutOfRangeException(nameof(configure), options.Version, "Unsupported Asaas version.");
        }

        builder.Services.AddMappers(o => o.FromAssembly(typeof(AsaasServiceCollectionExtensions).Assembly));

        var baseUrl = options.BaseUrl ?? AsaasDefaults.BaseUrl;
        builder.Services.AddHttpClient(AsaasDefaults.HttpClientName, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.TryAddWithoutValidation("access_token", options.ApiKey);
            http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", AsaasDefaults.UserAgent);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var info = new ProviderInfo(AsaasDefaults.ProviderName, AsaasDefaults.VersionString(AsaasApiVersion.V3));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var http = httpFactory.CreateClient(AsaasDefaults.HttpClientName);
            return new AsaasV3Provider(new AsaasV3Client(http), mappers);
        }, asDefault);
    }
}
