using System.Net.Http.Headers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace eQuantic.Payment.Tests.Fakes;

/// <summary>
/// stripe-mock in a container: Stripe's own check of a request's parameters, against the OpenAPI schema its release
/// bundles. It ignores <c>Stripe-Version</c>, so the image's version is what fixes the schema, and it is pinned:
/// v0.206.0 (2026-10-01), the first release after 2026-09-30.endive, whose schema refuses <c>payment_method_types</c>
/// on PaymentIntents and SetupIntents. Moving it is moving the schema the tests check against.
/// </summary>
public sealed class StripeMock : IAsyncLifetime
{
    public const string Image = "stripe/stripe-mock:v0.206.0";

    private const string ApiKey = "sk_test_123";
    private const ushort Port = 12111;

    private readonly IContainer _container = new ContainerBuilder(Image)
        .WithPortBinding(Port, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
            .ForPort(Port)
            .ForPath("/v1/customers")
            .WithHeader("Authorization", $"Bearer {ApiKey}")))
        .Build();

    /// <summary>A client for stripe-mock's API, authenticated as Stripe's clients are.</summary>
    public HttpClient Http() => new()
    {
        BaseAddress = new Uri($"http://{_container.Hostname}:{_container.GetMappedPublicPort(Port)}/v1/"),
        DefaultRequestHeaders = { Authorization = new AuthenticationHeaderValue("Bearer", ApiKey) },
    };

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
