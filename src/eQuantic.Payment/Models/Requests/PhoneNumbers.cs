using System.Text;

namespace eQuantic.Payment.Models.Requests;

/// <summary>Phone numbers as gateways take them: <c>+</c> and 7 to 15 digits (E.164).</summary>
internal static class PhoneNumbers
{
    // What a customer types between the digits of a phone number.
    private const string Separators = " .-()/";

    /// <summary>
    /// <paramref name="typed"/> in international form, or null when it is not a phone number. Separators are dropped,
    /// a leading <c>00</c> is read as <c>+</c>, and a number of <paramref name="nationalLength"/> digits without a
    /// country code is given <paramref name="callingCode"/>.
    /// </summary>
    public static string? International(string? typed, string callingCode, int nationalLength)
    {
        var text = typed?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        var international = text.StartsWith('+');
        var digits = new StringBuilder(text.Length);
        foreach (var character in international ? text[1..] : text)
        {
            if (char.IsAsciiDigit(character))
            {
                digits.Append(character);
            }
            else if (!Separators.Contains(character))
            {
                return null;
            }
        }

        var number = digits.ToString();
        if (!international && number.StartsWith("00", StringComparison.Ordinal))
        {
            number = number[2..];
            international = true;
        }

        if (!international)
        {
            if (number.Length != nationalLength)
            {
                return null;
            }

            number = callingCode + number;
        }

        // A country calling code never starts with 0.
        return number.Length is >= 7 and <= 15 && number[0] != '0' ? $"+{number}" : null;
    }
}
