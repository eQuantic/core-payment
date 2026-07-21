using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Http;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4;

/// <summary>
/// Typed client for the legacy Pagar.me API v4 (<c>https://api.pagar.me/1</c>).
/// Transaction-based: a single <c>transactions</c> resource. Auth is by <c>api_key</c> in the body.
/// </summary>
public class PagarmeClientV4(HttpClient httpClient, string apiKey) : PaymentHttpClientBase(httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    private readonly string _apiKey = apiKey;

    public Task<ApiResult<V4TransactionResponse>> CreateTransactionAsync(V4TransactionRequest request, CancellationToken cancellationToken = default)
    {
        request.ApiKey = _apiKey;
        return SendJsonAsync<V4TransactionResponse>(HttpMethod.Post, "transactions", request, cancellationToken);
    }

    public Task<ApiResult<V4TransactionResponse>> GetTransactionAsync(long transactionId, CancellationToken cancellationToken = default)
        => SendJsonAsync<V4TransactionResponse>(HttpMethod.Get, $"transactions/{transactionId}?api_key={_apiKey}", null, cancellationToken);

    public Task<ApiResult<V4TransactionResponse>> CaptureTransactionAsync(long transactionId, long? amount = null, CancellationToken cancellationToken = default)
        => SendJsonAsync<V4TransactionResponse>(HttpMethod.Post, $"transactions/{transactionId}/capture", new V4CaptureRequest { ApiKey = _apiKey, Amount = amount }, cancellationToken);

    public Task<ApiResult<V4TransactionResponse>> RefundTransactionAsync(long transactionId, long? amount = null, CancellationToken cancellationToken = default)
        => SendJsonAsync<V4TransactionResponse>(HttpMethod.Post, $"transactions/{transactionId}/refund", new V4RefundRequest { ApiKey = _apiKey, Amount = amount }, cancellationToken);

    public Task<ApiResult<V4CustomerResponse>> CreateCustomerAsync(V4CustomerRequest request, CancellationToken cancellationToken = default)
    {
        request.ApiKey = _apiKey;
        return SendJsonAsync<V4CustomerResponse>(HttpMethod.Post, "customers", request, cancellationToken);
    }

    public Task<ApiResult<V4CustomerResponse>> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default)
        => SendJsonAsync<V4CustomerResponse>(HttpMethod.Get, $"customers/{customerId}?api_key={_apiKey}", null, cancellationToken);
}
