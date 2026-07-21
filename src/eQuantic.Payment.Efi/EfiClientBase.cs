using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Http;

namespace eQuantic.Payment.Efi;

/// <summary>
/// Base for the Efí typed clients. Attaches a fresh OAuth Bearer token (from <see cref="EfiTokenProvider"/>)
/// on every request. Subclasses set the JSON naming policy for their product (Pix = Portuguese camelCase,
/// Cobranças = snake_case English).
/// </summary>
public abstract class EfiClientBase(HttpClient httpClient, EfiTokenProvider tokenProvider) : PaymentHttpClientBase(httpClient)
{
    protected async Task<ApiResult<TResponse>> SendAuthedAsync<TResponse>(
        HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken).ConfigureAwait(false);

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        return await SendAsync<TResponse>(request, cancellationToken).ConfigureAwait(false);
    }

    protected static JsonSerializerOptions CreateOptions(JsonNamingPolicy? namingPolicy) => new()
    {
        PropertyNamingPolicy = namingPolicy,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}
