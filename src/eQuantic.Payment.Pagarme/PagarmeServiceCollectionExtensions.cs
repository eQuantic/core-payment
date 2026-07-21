using System.Net.Http.Headers;
using System.Text;
using eQuantic.Mapper;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using eQuantic.Payment.Pagarme.V4;
using eQuantic.Payment.Pagarme.V5;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Pagarme;

public static class PagarmeServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Pagar.me provider. The concrete client (v4 or v5) is selected by
    /// <see cref="PagarmeOptions.Version"/>, so callers depend only on the unified <c>IPaymentProvider</c>.
    /// </summary>
    public static PaymentBuilder AddPagarme(this PaymentBuilder builder, Action<PagarmeOptions> configure, bool asDefault = false)
    {
        var options = new PagarmeOptions { ApiKey = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException("PagarmeOptions.ApiKey is required.", nameof(configure));
        }

        // Register this assembly's eQuantic.Mapper mappers (idempotent for the factory itself).
        builder.Services.AddMappers(o => o.FromAssembly(typeof(PagarmeServiceCollectionExtensions).Assembly));

        return options.Version switch
        {
            PagarmeApiVersion.V5 => AddV5(builder, options, asDefault),
            PagarmeApiVersion.V4 => AddV4(builder, options, asDefault),
            _ => throw new ArgumentOutOfRangeException(nameof(configure), options.Version, "Unsupported Pagar.me version."),
        };
    }

    private static PaymentBuilder AddV5(PaymentBuilder builder, PagarmeOptions options, bool asDefault)
    {
        var baseUrl = options.BaseUrl ?? PagarmeDefaults.V5BaseUrl;
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ApiKey}:"));

        builder.Services.AddHttpClient(PagarmeDefaults.V5HttpClientName, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var info = new ProviderInfo(PagarmeDefaults.ProviderName, PagarmeDefaults.VersionString(PagarmeApiVersion.V5));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var client = new PagarmeClientV5(httpFactory.CreateClient(PagarmeDefaults.V5HttpClientName));
            return new PagarmeProviderV5(client, mappers);
        }, asDefault);
    }

    private static PaymentBuilder AddV4(PaymentBuilder builder, PagarmeOptions options, bool asDefault)
    {
        var baseUrl = options.BaseUrl ?? PagarmeDefaults.V4BaseUrl;

        builder.Services.AddHttpClient(PagarmeDefaults.V4HttpClientName, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var info = new ProviderInfo(PagarmeDefaults.ProviderName, PagarmeDefaults.VersionString(PagarmeApiVersion.V4));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var client = new PagarmeClientV4(httpFactory.CreateClient(PagarmeDefaults.V4HttpClientName), options.ApiKey);
            return new PagarmeProviderV4(client, mappers);
        }, asDefault);
    }
}
