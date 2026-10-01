using System.Net;

namespace eQuantic.Payment.Tests.Fakes;

/// <summary>Records the last request and returns a queued canned response — no network involved.</summary>
public sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<(HttpStatusCode Status, string Body)> _responses = new();

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    public List<string> RequestPaths { get; } = [];

    /// <summary>Every request sent, in order.</summary>
    public List<HttpRequestMessage> Requests { get; } = [];

    /// <summary>The body of every request sent, in order; null for a request without one.</summary>
    public List<string?> RequestBodies { get; } = [];

    /// <summary>The value of header <paramref name="name"/> on the request at <paramref name="index"/>, or null when it was not sent.</summary>
    public string? Header(int index, string name) =>
        Requests[index].Headers.TryGetValues(name, out var values) ? values.Single() : null;

    public StubHttpMessageHandler EnqueueJson(HttpStatusCode status, string body)
    {
        _responses.Enqueue((status, body));
        return this;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        Requests.Add(request);
        RequestPaths.Add(request.RequestUri!.ToString());
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        RequestBodies.Add(LastRequestBody);

        var (status, body) = _responses.Count > 0 ? _responses.Dequeue() : (HttpStatusCode.OK, "{}");
        return new HttpResponseMessage(status)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json"),
        };
    }
}
