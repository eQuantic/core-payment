using System.Net.Http.Headers;
using eQuantic.Mapper;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using eQuantic.Payment.PagSeguro.Orders;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.PagSeguro;

public static class PagSeguroServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PagSeguro / PagBank provider (modern Orders/Charges API), so callers depend only on
    /// the unified <c>IPaymentProvider</c>.
    /// </summary>
    public static PaymentBuilder AddPagSeguro(this PaymentBuilder builder, Action<PagSeguroOptions> configure, bool asDefault = false)
    {
        var options = new PagSeguroOptions { Token = string.Empty };
        configure(options);

        if (string.IsNullOrWhiteSpace(options.Token))
        {
            throw new ArgumentException("PagSeguroOptions.Token is required.", nameof(configure));
        }

        builder.Services.AddMappers(o => o.FromAssembly(typeof(PagSeguroServiceCollectionExtensions).Assembly));

        var baseUrl = options.BaseUrl ?? PagSeguroDefaults.BaseUrl;
        builder.Services.AddHttpClient(PagSeguroDefaults.OrdersHttpClientName, http =>
        {
            http.BaseAddress = new Uri(baseUrl);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Token);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        });

        var info = new ProviderInfo(PagSeguroDefaults.ProviderName, PagSeguroDefaults.VersionString(PagSeguroApiVersion.Orders));
        return builder.AddProvider(info, sp =>
        {
            var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
            var mappers = sp.GetRequiredService<IMapperFactory>();
            var client = new PagSeguroOrdersClient(httpFactory.CreateClient(PagSeguroDefaults.OrdersHttpClientName));
            return new PagSeguroOrdersProvider(client, mappers);
        }, asDefault);
    }
}
