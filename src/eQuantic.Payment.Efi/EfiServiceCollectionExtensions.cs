using System.Net.Http.Headers;
using eQuantic.Mapper;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Efi.Cobrancas;
using eQuantic.Payment.Efi.Pix;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Efi;

public static class EfiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Efí provider. The product (<see cref="EfiApiVersion.Pix"/> or
    /// <see cref="EfiApiVersion.Cobrancas"/>) is selected by <see cref="EfiOptions.Version"/>. The Pix API
    /// requires <see cref="EfiOptions.Certificate"/> (mTLS), attached to the HTTP handler.
    /// </summary>
    public static PaymentBuilder AddEfi(this PaymentBuilder builder, Action<EfiOptions> configure, bool asDefault = false)
    {
        var options = new EfiOptions { ClientId = string.Empty, ClientSecret = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.ClientId) || string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            throw new ArgumentException("EfiOptions.ClientId and ClientSecret are required.", nameof(configure));
        }

        builder.Services.AddMappers(o => o.FromAssembly(typeof(EfiServiceCollectionExtensions).Assembly));

        return options.Version switch
        {
            EfiApiVersion.Pix => AddPix(builder, options, asDefault),
            EfiApiVersion.Cobrancas => AddCobrancas(builder, options, asDefault),
            _ => throw new ArgumentOutOfRangeException(nameof(configure), options.Version, "Unsupported Efí version."),
        };
    }

    private static PaymentBuilder AddPix(PaymentBuilder builder, EfiOptions options, bool asDefault)
    {
        if (options.Certificate is null)
        {
            throw new ArgumentException("EfiOptions.Certificate (mTLS) is required for the Efí Pix API.", nameof(options));
        }

        var baseUrl = options.BaseUrl ?? EfiDefaults.BaseUrlFor(EfiApiVersion.Pix, options.Sandbox);
        var clientId = options.ClientId;
        var clientSecret = options.ClientSecret;
        var certificate = options.Certificate;
        var pixKey = options.PixKey;

        ConfigureClientWithCertificate(builder, EfiDefaults.PixTokenHttpClientName, baseUrl, certificate);
        ConfigureClientWithCertificate(builder, EfiDefaults.PixApiHttpClientName, baseUrl, certificate);

        var info = new ProviderInfo(EfiDefaults.ProviderName, EfiDefaults.VersionString(EfiApiVersion.Pix));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var tokenProvider = new EfiTokenProvider(httpFactory.CreateClient(EfiDefaults.PixTokenHttpClientName), clientId, clientSecret, EfiDefaults.PixTokenPath);
            var client = new EfiPixClient(httpFactory.CreateClient(EfiDefaults.PixApiHttpClientName), tokenProvider);
            return new EfiPixProvider(client, mappers, pixKey);
        }, asDefault);
    }

    private static PaymentBuilder AddCobrancas(PaymentBuilder builder, EfiOptions options, bool asDefault)
    {
        var baseUrl = options.BaseUrl ?? EfiDefaults.BaseUrlFor(EfiApiVersion.Cobrancas, options.Sandbox);
        var clientId = options.ClientId;
        var clientSecret = options.ClientSecret;

        ConfigureClient(builder, EfiDefaults.CobrancasTokenHttpClientName, baseUrl);
        ConfigureClient(builder, EfiDefaults.CobrancasApiHttpClientName, baseUrl);

        var info = new ProviderInfo(EfiDefaults.ProviderName, EfiDefaults.VersionString(EfiApiVersion.Cobrancas));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var tokenProvider = new EfiTokenProvider(httpFactory.CreateClient(EfiDefaults.CobrancasTokenHttpClientName), clientId, clientSecret, EfiDefaults.CobrancasTokenPath);
            var client = new EfiCobrancasClient(httpFactory.CreateClient(EfiDefaults.CobrancasApiHttpClientName), tokenProvider);
            return new EfiCobrancasProvider(client, mappers);
        }, asDefault);
    }

    private static void ConfigureClient(PaymentBuilder builder, string name, string baseUrl)
    {
        builder.Services.AddHttpClient(name, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }

    private static void ConfigureClientWithCertificate(PaymentBuilder builder, string name, string baseUrl, System.Security.Cryptography.X509Certificates.X509Certificate2 certificate)
    {
        builder.Services.AddHttpClient(name, http =>
            {
                http.BaseAddress = new Uri(baseUrl);
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(certificate);
                return handler;
            });
    }
}
