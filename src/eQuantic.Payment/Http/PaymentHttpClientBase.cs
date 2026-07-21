using System.Text;
using System.Text.Json;

namespace eQuantic.Payment.Http;

/// <summary>
/// Base class for versioned provider API clients. Handles serialization,
/// raw-body capture and status propagation without throwing on API errors.
/// </summary>
public abstract class PaymentHttpClientBase(HttpClient httpClient)
{
    protected HttpClient HttpClient { get; } = httpClient;

    /// <summary>Serializer options matching the provider wire format (snake_case for most gateways).</summary>
    protected abstract JsonSerializerOptions JsonOptions { get; }

    protected async Task<ApiResult<TResponse>> SendAsync<TResponse>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var rawBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        TResponse? data = default;
        if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(rawBody))
        {
            data = JsonSerializer.Deserialize<TResponse>(rawBody, JsonOptions);
        }

        return new ApiResult<TResponse>
        {
            StatusCode = (int)response.StatusCode,
            Data = data,
            RawBody = rawBody,
        };
    }

    /// <summary>Sends a JSON request (the wire format of Pagar.me and most Brazilian gateways).</summary>
    protected Task<ApiResult<TResponse>> SendJsonAsync<TResponse>(
        HttpMethod method, string path, object? body = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        return SendAsync<TResponse>(request, cancellationToken);
    }

    /// <summary>Sends a form-urlencoded request (the wire format of Stripe).</summary>
    protected Task<ApiResult<TResponse>> SendFormAsync<TResponse>(
        HttpMethod method, string path, IEnumerable<KeyValuePair<string, string>>? form = null, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(method, path);
        if (form is not null)
        {
            request.Content = new FormUrlEncodedContent(form);
        }

        return SendAsync<TResponse>(request, cancellationToken);
    }
}
