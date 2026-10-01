using System.Text.Json;
using System.Text.Json.Nodes;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>What a raw Stripe body keeps for auditing, once the secrets that act on Stripe are out of it.</summary>
internal static class StripeRawBody
{
    private const string Redacted = "[redacted]";

    /// <summary>
    /// <paramref name="raw"/> with every <c>client_secret</c> replaced, at any depth (an error carries its SetupIntent):
    /// a client secret confirms the setup it belongs to, so it reaches the customer's browser only, through the
    /// result, never an audit trail. A body that names one but cannot be read is not kept at all.
    /// </summary>
    public static string? WithoutClientSecrets(string? raw)
    {
        if (string.IsNullOrEmpty(raw) || !raw.Contains("client_secret", StringComparison.Ordinal))
        {
            return raw;
        }

        try
        {
            var node = JsonNode.Parse(raw);
            Redact(node);
            return node?.ToJsonString();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Redact(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject properties:
                foreach (var (name, value) in properties.ToList())
                {
                    if (name == "client_secret" && value is JsonValue)
                    {
                        properties[name] = Redacted;
                    }
                    else
                    {
                        Redact(value);
                    }
                }

                break;
            case JsonArray items:
                foreach (var item in items)
                {
                    Redact(item);
                }

                break;
        }
    }
}
