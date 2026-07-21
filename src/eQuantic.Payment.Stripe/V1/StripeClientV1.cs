using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>
/// Typed client for the Stripe v1 REST API (<c>https://api.stripe.com/v1</c>).
/// Uses the PaymentIntents resource, which supports cards, PIX and boleto for Brazil.
/// Requests are form-urlencoded; auth is a Bearer secret key with a pinned <c>Stripe-Version</c> header.
/// PaymentIntent calls always expand <c>latest_charge</c> so card/PIX/boleto details are returned inline.
/// </summary>
public class StripeClientV1(HttpClient httpClient) : PaymentHttpClientBase(httpClient)
{
    private const string ExpandLatestCharge = "expand[]=latest_charge";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    public Task<ApiResult<StripePaymentIntent>> CreatePaymentIntentAsync(IEnumerable<KeyValuePair<string, string>> form, CancellationToken cancellationToken = default)
        => SendFormAsync<StripePaymentIntent>(HttpMethod.Post, "payment_intents", WithLatestChargeExpand(form), cancellationToken);

    public Task<ApiResult<StripePaymentIntent>> GetPaymentIntentAsync(string id, CancellationToken cancellationToken = default)
        => SendFormAsync<StripePaymentIntent>(HttpMethod.Get, $"payment_intents/{id}?{ExpandLatestCharge}", form: null, cancellationToken);

    public Task<ApiResult<StripePaymentIntent>> CapturePaymentIntentAsync(string id, IEnumerable<KeyValuePair<string, string>>? form = null, CancellationToken cancellationToken = default)
        => SendFormAsync<StripePaymentIntent>(HttpMethod.Post, $"payment_intents/{id}/capture", WithLatestChargeExpand(form), cancellationToken);

    public Task<ApiResult<StripePaymentIntent>> CancelPaymentIntentAsync(string id, CancellationToken cancellationToken = default)
        => SendFormAsync<StripePaymentIntent>(HttpMethod.Post, $"payment_intents/{id}/cancel", WithLatestChargeExpand(form: null), cancellationToken);

    public Task<ApiResult<StripeRefund>> CreateRefundAsync(IEnumerable<KeyValuePair<string, string>> form, CancellationToken cancellationToken = default)
        => SendFormAsync<StripeRefund>(HttpMethod.Post, "refunds", form, cancellationToken);

    public Task<ApiResult<StripeCustomer>> CreateCustomerAsync(IEnumerable<KeyValuePair<string, string>> form, CancellationToken cancellationToken = default)
        => SendFormAsync<StripeCustomer>(HttpMethod.Post, "customers", form, cancellationToken);

    public Task<ApiResult<StripeCustomer>> GetCustomerAsync(string id, CancellationToken cancellationToken = default)
        => SendFormAsync<StripeCustomer>(HttpMethod.Get, $"customers/{id}", form: null, cancellationToken);

    private static List<KeyValuePair<string, string>> WithLatestChargeExpand(IEnumerable<KeyValuePair<string, string>>? form)
    {
        var list = form is null ? [] : new List<KeyValuePair<string, string>>(form);
        list.Add(new KeyValuePair<string, string>("expand[]", "latest_charge"));
        return list;
    }
}
