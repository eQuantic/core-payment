namespace eQuantic.Payment.PagSeguro;

/// <summary>
/// PagSeguro / PagBank API surfaces. Only the modern JSON <see cref="Orders"/> API is implemented; the
/// legacy XML API (<c>ws.pagseguro.uol.com.br</c>) is a separate, incompatible version axis, not modeled.
/// </summary>
public enum PagSeguroApiVersion
{
    /// <summary>Modern Orders/Charges API (<c>https://api.pagseguro.com</c>).</summary>
    Orders,
}

public sealed class PagSeguroOptions
{
    /// <summary>Bearer token from the PagBank developer portal / dashboard.</summary>
    public required string Token { get; set; }

    public PagSeguroApiVersion Version { get; set; } = PagSeguroApiVersion.Orders;

    /// <summary>Override the base URL (sandbox is <c>https://sandbox.api.pagseguro.com/</c>). Defaults to production.</summary>
    public string? BaseUrl { get; set; }
}
