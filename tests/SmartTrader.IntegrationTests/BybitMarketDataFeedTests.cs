using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.MarketData.Bybit;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;
using SmartTrader.Infrastructure.MarketData.Bybit.Rest;
using SmartTrader.Infrastructure.MarketData.Bybit.WebSocket;
using SmartTrader.Infrastructure.MarketData.Bybit.Internal;

namespace SmartTrader.IntegrationTests;

public class BybitMarketDataFeedTests
{
    [Fact]
    public async Task GetHistoryAsync_PaginatesAndDeduplicates()
    {
        var restClient = new FakeRestClient();
        var feed = CreateFeed(restClient);

        var start = new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMinutes(3);

        var candles = await feed.GetHistoryAsync("BTCUSDT", Timeframe.M1, start, end, CancellationToken.None);

        Assert.Equal(3, candles.Count);
        Assert.Collection(candles,
            c => Assert.Equal(start, c.TsOpenUtc.UtcDateTime),
            c => Assert.Equal(start.AddMinutes(1), c.TsOpenUtc.UtcDateTime),
            c => Assert.Equal(start.AddMinutes(2), c.TsOpenUtc.UtcDateTime));
        Assert.Equal(2, restClient.Calls.Count);
    }

    private static BybitMarketDataFeed CreateFeed(IBybitRestClient restClient)
    {
        var options = Options.Create(new BybitOptions
        {
            Symbols = new[] { "BTCUSDT" },
            Timeframes = new[] { "1m" },
            Rest = new BybitRestOptions { PageSize = 2, BackfillDays = 1, TimeoutSec = 5, MaxConcurrency = 1 },
            Ws = new BybitWsOptions(),
            RateLimit = new BybitRateLimitOptions { RequestsPerMinute = 120, BurstSize = 10 }
        });

        return new BybitMarketDataFeed(
            restClient,
            new NullWebSocketClient(),
            new NoopRateLimiter(),
            options,
            NullLogger<BybitMarketDataFeed>.Instance);
    }

    private sealed class FakeRestClient : IBybitRestClient
    {
        private readonly List<List<Candle>> _pages;
        private int _current;

        public List<(DateTime Start, DateTime End)> Calls { get; } = new();

        public FakeRestClient()
        {
            var baseTime = new DateTimeOffset(new DateTime(2024, 01, 01, 0, 0, 0, DateTimeKind.Utc));
            _pages = new List<List<Candle>>
            {
                new()
                {
                    new Candle(baseTime, 1, 2, 0.5m, 1.5m, 10),
                    new Candle(baseTime.AddMinutes(1), 2, 3, 1.5m, 2.8m, 11)
                },
                new()
                {
                    new Candle(baseTime.AddMinutes(1), 2, 3, 1.5m, 2.8m, 11),
                    new Candle(baseTime.AddMinutes(2), 3, 4, 2.5m, 3.5m, 12)
                }
            };
        }

        public Task<IReadOnlyList<Candle>> GetKlinesAsync(string symbol, Timeframe timeframe, DateTime startUtc, DateTime endUtc, int limit, CancellationToken cancellationToken)
        {
            Calls.Add((startUtc, endUtc));
            if (_current >= _pages.Count)
            {
                return Task.FromResult<IReadOnlyList<Candle>>(Array.Empty<Candle>());
            }

            return Task.FromResult<IReadOnlyList<Candle>>(_pages[_current++]);
        }
    }

    private sealed class NullWebSocketClient : IBybitWebSocketClient
    {
        public async IAsyncEnumerable<CandleEvent> SubscribeKlinesAsync(string symbol, Timeframe timeframe, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class NoopRateLimiter : IBybitRateLimiter
    {
        public Task WaitForSlotAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}









