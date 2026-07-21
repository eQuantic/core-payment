using System.Text.Json;
using System.Text.Json.Serialization;

namespace eQuantic.Payment.PagSeguro.Orders.Models;

/// <summary>
/// Reads a JSON value that PagSeguro returns inconsistently as a string or a number
/// (e.g. <c>payment_response.code</c> = <c>"20000"</c> or <c>20000</c>) into a C# string.
/// </summary>
public sealed class FlexibleStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetRawValue(),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            JsonTokenType.Null => null,
            _ => null,
        };

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value);
}

file static class Utf8JsonReaderExtensions
{
    public static string GetRawValue(this Utf8JsonReader reader)
        => reader.TryGetInt64(out var l) ? l.ToString() : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
}
