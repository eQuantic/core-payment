using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace eQuantic.Payment.Efi;

/// <summary>
/// Acquires and caches an Efí OAuth2 <c>client_credentials</c> access token. Thread-safe; refreshes shortly
/// before expiry. The supplied <see cref="HttpClient"/> must carry the mTLS certificate for the Pix API.
/// </summary>
public sealed class EfiTokenProvider(HttpClient httpClient, string clientId, string clientSecret, string tokenPath)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private readonly SemaphoreSlim _lock = new(1, 1);
    private string? _token;
    private DateTimeOffset _expiresAt;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
        {
            return _token;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_token is not null && DateTimeOffset.UtcNow < _expiresAt)
            {
                return _token;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, tokenPath);
            var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
            request.Content = new StringContent("""{"grant_type":"client_credentials"}""", Encoding.UTF8, "application/json");

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var token = JsonSerializer.Deserialize<EfiTokenResponse>(body, JsonOptions)
                ?? throw new InvalidOperationException("Efí returned an empty token response.");

            _token = token.AccessToken;
            // Refresh 30s before the reported expiry.
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(Math.Max(30, token.ExpiresIn - 30));
            return _token!;
        }
        finally
        {
            _lock.Release();
        }
    }

    private sealed class EfiTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
