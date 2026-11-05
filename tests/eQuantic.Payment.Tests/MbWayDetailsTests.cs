using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Tests;

/// <summary>The phone an MB WAY charge reaches any gateway with: what the customer typed, as <c>+</c> and digits.</summary>
public class MbWayDetailsTests
{
    [Theory]
    [InlineData("912345678", "+351912345678")]
    [InlineData("912 345 678", "+351912345678")]
    [InlineData("00351 912 345 678", "+351912345678")]
    [InlineData("+351 912-345-678", "+351912345678")]
    [InlineData("+44 7700 900123", "+447700900123")]
    [InlineData("0044 7700 900123", "+447700900123")]
    public void A_phone_number_as_typed_is_written_in_international_form(string typed, string international)
    {
        Assert.Equal(international, new MbWayDetails { PhoneNumber = typed }.InternationalPhoneNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("12345")]
    [InlineData("351912345678")]
    [InlineData("+351 912 345 678 ext 2")]
    [InlineData("+0351912345678")]
    [InlineData("+1234567890123456")]
    public void What_is_not_a_phone_number_has_no_international_form(string typed)
    {
        Assert.Null(new MbWayDetails { PhoneNumber = typed }.InternationalPhoneNumber);
    }
}
