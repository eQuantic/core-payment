namespace eQuantic.Payment.Exceptions;

/// <summary>Thrown for configuration/registration errors (API errors are returned in the response envelope, never thrown).</summary>
public class PaymentException(string message, Exception? innerException = null)
    : Exception(message, innerException);

/// <summary>Thrown when resolving a provider/version that was not registered.</summary>
public sealed class ProviderNotRegisteredException(string providerKey)
    : PaymentException($"Payment provider '{providerKey}' is not registered. Register it with AddPayments(...).")
{
    public string ProviderKey { get; } = providerKey;
}
