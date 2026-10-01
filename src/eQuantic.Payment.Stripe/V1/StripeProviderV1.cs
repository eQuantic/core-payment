using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over <see cref="StripeClientV1"/>.</summary>
public sealed class StripeProviderV1 : IPaymentProvider
{
    /// <summary>A provider without a webhook signing secret: it refuses every notification.</summary>
    public StripeProviderV1(StripeClientV1 client, StripeApiVersion version, IMapperFactory mappers)
        : this(client, version, mappers, webhookSecret: null, StripeDefaults.WebhookTolerance, TimeProvider.System)
    {
    }

    /// <summary>
    /// A provider that verifies notifications with the endpoint's signing secret (<c>whsec_xxx</c>), refusing
    /// any signed further from <paramref name="clock"/>'s now than <paramref name="webhookTolerance"/>.
    /// </summary>
    public StripeProviderV1(
        StripeClientV1 client,
        StripeApiVersion version,
        IMapperFactory mappers,
        string? webhookSecret,
        TimeSpan webhookTolerance,
        TimeProvider clock)
    {
        Info = new ProviderInfo(StripeDefaults.ProviderName, StripeDefaults.VersionString(version));
        var operations = new StripeV1Operations(client, Info, mappers, clock);
        Charges = operations;
        Refunds = operations;
        Customers = operations;
        Notifications = new StripeNotificationOperations(Info, webhookSecret, webhookTolerance, clock);
        PaymentMethods = new StripePaymentMethodOperations(client, Info, mappers);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
    public INotificationOperations Notifications { get; }
    public IPaymentMethodOperations PaymentMethods { get; }
}
