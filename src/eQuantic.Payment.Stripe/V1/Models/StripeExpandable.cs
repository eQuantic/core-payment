using System.Text.Json;
using System.Text.Json.Serialization;

namespace eQuantic.Payment.Stripe.V1.Models;

/// <summary>
/// Stripe "expandable" fields arrive as a bare id string by default, or as the full nested object
/// when requested via <c>expand[]</c>. This converter accepts either: a string becomes a
/// <see cref="StripeCharge"/> carrying only its <see cref="StripeCharge.Id"/>; an object is
/// deserialized in full. Applied to <see cref="StripePaymentIntent.LatestCharge"/>.
/// </summary>
public sealed class StripeChargeExpandableConverter : JsonConverter<StripeCharge>
{
    public override StripeCharge? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => new StripeCharge { Id = reader.GetString() ?? string.Empty },
            JsonTokenType.StartObject => JsonSerializer.Deserialize<StripeCharge>(ref reader, WithoutThisConverter(options)),
            _ => null,
        };

    public override void Write(Utf8JsonWriter writer, StripeCharge value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.Id);

    // Deserialize the object with the type's default handling (no property-level converter in play),
    // which cannot re-enter this converter — so recursion is not possible here. Kept explicit for clarity.
    private static JsonSerializerOptions WithoutThisConverter(JsonSerializerOptions options) => options;
}
