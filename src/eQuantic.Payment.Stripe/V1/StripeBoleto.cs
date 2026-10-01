using System.Globalization;
using System.Text;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>
/// What Stripe's boleto expects. Its days are São Paulo's: a voucher created on Monday with
/// <c>expires_after_days</c> 2 stays payable until Wednesday, 23:59 in São Paulo. And it takes the payer's name
/// and address in ASCII.
/// </summary>
internal static class StripeBoleto
{
    // São Paulo has kept UTC−3 all year since Brazil ended daylight saving time in 2019.
    private static readonly TimeSpan SaoPaulo = TimeSpan.FromHours(-3);

    /// <summary>The calendar day it is in São Paulo at <paramref name="now"/>.</summary>
    public static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.ToOffset(SaoPaulo).DateTime);

    /// <summary>The São Paulo calendar day of an instant Stripe sent, such as a voucher's <c>expires_at</c>.</summary>
    public static DateOnly DayOf(long unixSeconds) => Today(DateTimeOffset.FromUnixTimeSeconds(unixSeconds));

    /// <summary>
    /// <paramref name="value"/> in ASCII: accents dropped (<c>São João</c> becomes <c>Sao Joao</c>), compatibility
    /// forms folded (<c>nº</c> becomes <c>no</c>), and anything else outside ASCII removed.
    /// </summary>
    public static string Ascii(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormKD);
        var ascii = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (character <= '\u007F' && CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                ascii.Append(character);
            }
        }

        return ascii.ToString();
    }
}
