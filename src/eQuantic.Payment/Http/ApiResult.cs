namespace eQuantic.Payment.Http;

/// <summary>Low-level HTTP result shared by all provider clients.</summary>
public sealed class ApiResult<T>
{
    public required int StatusCode { get; init; }

    public bool IsSuccess => StatusCode is >= 200 and < 300;

    /// <summary>Deserialized body when the call succeeded.</summary>
    public T? Data { get; init; }

    /// <summary>Raw response body (success or error), preserved for auditing.</summary>
    public string? RawBody { get; init; }
}
