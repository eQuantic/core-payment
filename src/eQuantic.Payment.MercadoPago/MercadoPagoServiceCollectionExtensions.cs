using System.Net.Http.Headers;
using eQuantic.Mapper;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.MercadoPago.Orders;
using eQuantic.Payment.MercadoPago.Payments;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.MercadoPago;

public static class MercadoPagoServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Mercado Pago provider. The API surface (Payments or Orders) is selected by
    /// <see cref="MercadoPagoOptions.Version"/>, so callers depend only on the unified <c>IPaymentProvider</c>.
    /// </summary>
    public static PaymentBuilder AddMercadoPago(this PaymentBuilder builder, Action<MercadoPagoOptions> configure, bool asDefault = false)
    {
        var options = new MercadoPagoOptions { AccessToken = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.AccessToken))
        {
            throw new ArgumentException("MercadoPagoOptions.AccessToken is required.", nameof(configure));
        }

        builder.Services.AddMappers(o => o.FromAssembly(typeof(MercadoPagoServiceCollectionExtensions).Assembly));

        return options.Version switch
        {
            MercadoPagoApiVersion.Payments => AddPayments(builder, options, asDefault),
            MercadoPagoApiVersion.Orders => AddOrders(builder, options, asDefault),
            _ => throw new ArgumentOutOfRangeException(nameof(configure), options.Version, "Unsupported Mercado Pago version."),
        };
    }

    private static PaymentBuilder AddPayments(PaymentBuilder builder, MercadoPagoOptions options, bool asDefault)
    {
        ConfigureHttpClient(builder, MercadoPagoDefaults.PaymentsHttpClientName, options);

        var info = new ProviderInfo(MercadoPagoDefaults.ProviderName, MercadoPagoDefaults.VersionString(MercadoPagoApiVersion.Payments));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var http = httpFactory.CreateClient(MercadoPagoDefaults.PaymentsHttpClientName);
            return new MercadoPagoPaymentsProvider(new MercadoPagoPaymentsClient(http), new MercadoPagoCustomerClient(http), mappers);
        }, asDefault);
    }

    private static PaymentBuilder AddOrders(PaymentBuilder builder, MercadoPagoOptions options, bool asDefault)
    {
        ConfigureHttpClient(builder, MercadoPagoDefaults.OrdersHttpClientName, options);

        var info = new ProviderInfo(MercadoPagoDefaults.ProviderName, MercadoPagoDefaults.VersionString(MercadoPagoApiVersion.Orders));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var http = httpFactory.CreateClient(MercadoPagoDefaults.OrdersHttpClientName);
            return new MercadoPagoOrdersProvider(new MercadoPagoOrdersClient(http), new MercadoPagoCustomerClient(http), mappers);
        }, asDefault);
    }

    private static void ConfigureHttpClient(PaymentBuilder builder, string name, MercadoPagoOptions options)
    {
        var baseUrl = options.BaseUrl ?? MercadoPagoDefaults.BaseUrl;
        builder.Services.AddHttpClient(name, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.AccessToken);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
