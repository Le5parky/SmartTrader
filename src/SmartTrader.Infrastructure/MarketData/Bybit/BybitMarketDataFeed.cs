using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.MarketData.Bybit.Internal;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;
using SmartTrader.Infrastructure.MarketData.Bybit.Rest;
using SmartTrader.Infrastructure.MarketData.Bybit.WebSocket;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Infrastructure.MarketData.Bybit;

internal sealed class BybitMarketDataFeed : IMarketDataFeed
{
    private readonly IBybitRestClient _restClient;
    private readonly IBybitWebSocketClient _webSocketClient;
    private readonly IBybitRateLimiter _rateLimiter;
    private readonly ILogger<BybitMarketDataFeed> _logger;
    private readonly BybitOptions _options;

    public BybitMarketDataFeed(
        IBybitRestClient restClient,
        IBybitWebSocketClient webSocketClient,
        IBybitRateLimiter rateLimiter,
        IOptions<BybitOptions> options,
        ILogger<BybitMarketDataFeed> logger)
    {
        _restClient = restClient;
        _webSocketClient = webSocketClient;
        _rateLimiter = rateLimiter;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<Candle>> GetHistoryAsync(
        string symbol,
        Timeframe timeframe,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        if (fromUtc >= toUtc)
        {
            return Array.Empty<Candle>();
        }

        var normalizedStart = timeframe.AlignToFrame(DateTime.SpecifyKind(fromUtc, DateTimeKind.Utc));
        var normalizedEnd = timeframe.AlignToFrame(DateTime.SpecifyKind(toUtc, DateTimeKind.Utc));
        if (normalizedEnd <= normalizedStart)
        {
            normalizedEnd = normalizedStart + timeframe.ToTimeSpan();
        }

        var pageSize = Math.Clamp(_options.Rest.PageSize, 1, 1000);
        var step = timeframe.ToTimeSpan() * pageSize;

        var cursor = normalizedStart;
        var dedupe = new HashSet<DateTimeOffset>();
        var result = new List<Candle>();

        while (cursor < normalizedEnd && !cancellationToken.IsCancellationRequested)
        {
            var next = cursor + step;
            if (next > normalizedEnd)
            {
                next = normalizedEnd;
            }

            await _rateLimiter.WaitForSlotAsync(cancellationToken).ConfigureAwait(false);

            var candles = await _restClient
                .GetKlinesAsync(symbol, timeframe, cursor, next, pageSize, cancellationToken)
                .ConfigureAwait(false);

            if (candles.Count == 0)
            {
                cursor = next;
                continue;
            }

            foreach (var candle in candles)
            {
                var ts = candle.TsOpenUtc;
                if (ts < normalizedStart || ts >= normalizedEnd)
                {
                    continue;
                }

                if (dedupe.Add(ts))
                {
                    result.Add(candle);
                }
            }

            var last = candles[^1].TsOpenUtc.UtcDateTime;
            if (last > cursor)
            {
                cursor = last + timeframe.ToTimeSpan();
            }
            else
            {
                cursor = next;
            }
        }

        result.Sort(static (a, b) => a.TsOpenUtc.CompareTo(b.TsOpenUtc));

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Fetched {count} candles for {symbol}/{timeframe} between {from} and {to}",
                result.Count,
                symbol,
                timeframe,
                normalizedStart,
                normalizedEnd);
        }

        return result;
    }

    public IAsyncEnumerable<CandleEvent> StreamKlinesAsync(
        string symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken)
    {
        return _webSocketClient.SubscribeKlinesAsync(symbol, timeframe, cancellationToken);
    }
}


