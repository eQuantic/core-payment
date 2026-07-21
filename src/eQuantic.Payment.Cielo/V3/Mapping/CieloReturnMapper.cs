using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Cielo.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Cielo.V3.Mapping;

/// <summary>
/// Maps the flat capture/void response to the unified <see cref="Charge"/>. That response carries only the
/// resulting status and acquirer codes (no id/amount), so the operations layer injects the known charge id
/// and captured amount afterwards.
/// </summary>
public sealed class CieloReturnMapper : IMapper<CieloReturnResponse, Charge>
{
    public Charge? Map(CieloReturnResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        return new Charge
        {
            Id = string.Empty,
            Status = CieloStatusMapper.FromCode(source.Status),
            ProviderStatus = source.Status.ToString(CultureInfo.InvariantCulture),
            Amount = Money.FromCents(0),
            Method = PaymentMethodType.CreditCard,
            Card = string.IsNullOrEmpty(source.AuthorizationCode)
                ? null
                : new CardOutput { AuthorizationCode = source.AuthorizationCode },
        };
    }

    public Charge? Map(CieloReturnResponse? source, Charge? destination) => Map(source);
}
