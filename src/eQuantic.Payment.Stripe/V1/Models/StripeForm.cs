namespace eQuantic.Payment.Stripe.V1.Models;

/// <summary>
/// A Stripe form-urlencoded request body, as an ordered list of key/value pairs
/// (Stripe uses bracket notation for nested keys, e.g. <c>payment_method_data[billing_details][address][line1]</c>).
/// Used as the destination type of the request mappers so form-building runs through eQuantic.Mapper.
/// </summary>
public sealed class StripeForm : List<KeyValuePair<string, string>>
{
    /// <summary>Appends a key/value pair.</summary>
    public void Add(string key, string value) => Add(new KeyValuePair<string, string>(key, value));
}
