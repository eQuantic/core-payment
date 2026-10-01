namespace eQuantic.Payment.Models.Requests;

/// <summary>
/// A notification exactly as it arrived: the raw body, character for character, and its headers. Signatures
/// are computed over the raw body, so it must not be parsed and serialized again before it is verified.
/// </summary>
public sealed class NotificationRequest
{
    public required string Body { get; init; }

    /// <summary>The request's headers; names are matched without regard to case.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }
}
