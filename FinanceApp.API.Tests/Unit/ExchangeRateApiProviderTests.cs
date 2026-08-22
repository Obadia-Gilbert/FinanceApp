using System.Net;
using FinanceApp.Domain.Enums;
using FinanceApp.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinanceApp.API.Tests.Unit;

/// <summary>
/// Covers <see cref="ExchangeRateApiProvider"/>'s HTTP + JSON handling in isolation, via a
/// stub <see cref="HttpMessageHandler"/> — no real network call, no new test package.
/// The provider must never throw: any failure (bad status, bad payload, transport
/// exception) is expected to surface as a <c>null</c> result so the background job and
/// the store's fallback tiers can absorb it.
/// </summary>
public class ExchangeRateApiProviderTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;
        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) => _respond = respond;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_respond(request));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("simulated transport failure");
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public StubHttpClientFactory(HttpMessageHandler handler) => _handler = handler;
        public HttpClient CreateClient(string name) => new(_handler);
    }

    private static ExchangeRateApiProvider Build(HttpMessageHandler handler)
    {
        var factory = new StubHttpClientFactory(handler);
        var options = Options.Create(new ExchangeRateSettings());
        return new ExchangeRateApiProvider(factory, options, NullLogger<ExchangeRateApiProvider>.Instance);
    }

    private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
        => new(status) { Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json") };

    [Fact]
    public async Task FetchRatesToUsdAsync_SuccessResponse_InvertsRatesToUsdConvention()
    {
        var sut = Build(new StubHandler(_ => JsonResponse(HttpStatusCode.OK,
            """{"result":"success","rates":{"USD":1,"TZS":2630.5}}""")));

        var result = await sut.FetchRatesToUsdAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1m, result![Currency.USD]);
        Assert.Equal(1m / 2630.5m, result[Currency.TZS]);
    }

    [Fact]
    public async Task FetchRatesToUsdAsync_UnknownCurrencyCodes_AreIgnoredNotThrown()
    {
        var sut = Build(new StubHandler(_ => JsonResponse(HttpStatusCode.OK,
            """{"result":"success","rates":{"USD":1,"XYZ":5}}""")));

        var result = await sut.FetchRatesToUsdAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.ContainsKey(Currency.USD));
        Assert.DoesNotContain(result.Keys, c => c.ToString() == "XYZ");
    }

    [Fact]
    public async Task FetchRatesToUsdAsync_NonSuccessResult_ReturnsNull()
    {
        var sut = Build(new StubHandler(_ => JsonResponse(HttpStatusCode.OK, """{"result":"error"}""")));

        var result = await sut.FetchRatesToUsdAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchRatesToUsdAsync_Non200StatusCode_ReturnsNull()
    {
        var sut = Build(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var result = await sut.FetchRatesToUsdAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task FetchRatesToUsdAsync_HandlerThrows_ReturnsNullNotException()
    {
        var sut = Build(new ThrowingHandler());

        var result = await sut.FetchRatesToUsdAsync(CancellationToken.None);

        Assert.Null(result);
    }
}
