using SmartTrader.Worker.Strategies;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;
using SmartTrader.Infrastructure.Persistence.Repositories;
using SmartTrader.Trading.Abstractions.Models;
using StackExchange.Redis;

namespace SmartTrader.Worker.Workers;

public sealed class BybitIngestionWorker : BackgroundService
{
    private readonly IMarketDataFeed _feed;
    private readonly ICandleWriteRepository _writeRepository;
    private readonly ICandleReadRepository _readRepository;
    private readonly ILogger<BybitIngestionWorker> _logger;
    private readonly BybitOptions _options;
    private readonly Counter<long> _candlesIngested;
    private readonly Histogram<double> _ingestLatency;
    private readonly SemaphoreSlim _restGate;
    private readonly IDatabase? _redis;
    private readonly ConcurrentDictionary<(string Symbol, Timeframe Timeframe), KeyValuePair<string, object?>[]> _tagCache = new();
    private readonly StrategyEngine? _strategyEngine;

    public BybitIngestionWorker(
        IMarketDataFeed feed,
        ICandleWriteRepository writeRepository,
        ICandleReadRepository readRepository,
        IOptions<BybitOptions> options,
        ILogger<BybitIngestionWorker> logger,
        IMeterFactory meterFactory,
        IConnectionMultiplexer? connectionMultiplexer = null,
        StrategyEngine? strategyEngine = null)
    {
        _feed = feed;
        _writeRepository = writeRepository;
        _readRepository = readRepository;
        _logger = logger;
        _options = options.Value;
        _restGate = new SemaphoreSlim(Math.Max(1, _options.Rest.MaxConcurrency));
        _redis = connectionMultiplexer?.GetDatabase();
        _strategyEngine = strategyEngine;

        var meter = meterFactory.Create(new MeterOptions("SmartTrader.Bybit.Ingestion")
        {
            Tags = new KeyValuePair<string, object?>[]
            {
                new("component", "bybit-ingestion")
            }
        });
        _candlesIngested = meter.CreateCounter<long>("candles_ingested_total", unit: "candles", description: "Total number of candles ingested into persistence");
        _ingestLatency = meter.CreateHistogram<double>("ingest_latency_ms", unit: "ms", description: "Latency between candle open time and persistence");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timeframes = ResolveTimeframes();
        var tasks = new List<Task>();

        foreach (var symbol in _options.Symbols)
        {
            foreach (var timeframe in timeframes)
            {
                tasks.Add(RunSymbolAsync(symbol, timeframe, stoppingToken));
            }
        }

        await Task.WhenAll(tasks);
    }

    private IReadOnlyList<Timeframe> ResolveTimeframes()
    {
        var list = new List<Timeframe>(_options.Timeframes.Length);
        foreach (var tf in _options.Timeframes)
        {
            if (!TimeframeExtensions.TryParse(tf, out var parsed) || parsed is null)
            {
                throw new InvalidOperationException($"Unsupported timeframe '{tf}' in configuration.");
            }

            list.Add(parsed.Value);
        }

        return list;
    }

    private async Task RunSymbolAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken)
    {
        try
        {
            await BackfillAsync(symbol, timeframe, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backfill failed for {Symbol}/{Timeframe}", symbol, timeframe);
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var candleEvent in _feed.StreamKlinesAsync(symbol, timeframe, cancellationToken).ConfigureAwait(false))
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    if (candleEvent.IsClosed)
                    {
                        await PersistClosedCandleAsync(symbol, timeframe, candleEvent.Candle, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await CacheInProgressAsync(symbol, timeframe, candleEvent.Candle).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stream loop crashed for {Symbol}/{Timeframe}, retrying shortly", symbol, timeframe);
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task BackfillAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken)
    {
        var last = await _readRepository.GetLastCandleOpenAsync(symbol, timeframe, cancellationToken).ConfigureAwait(false);
        var frame = timeframe.ToTimeSpan();

        var start = last is not null
            ? last.Value.UtcDateTime + frame
            : DateTime.UtcNow - TimeSpan.FromDays(_options.Rest.BackfillDays);

        start = timeframe.AlignToFrame(start);
        var end = timeframe.AlignToFrame(DateTime.UtcNow);
        if (start >= end)
        {
            return;
        }

        await _restGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var history = await _feed.GetHistoryAsync(symbol, timeframe, start, end, cancellationToken).ConfigureAwait(false);
            if (history.Count == 0)
            {
                return;
            }

            await _writeRepository.UpsertAsync(symbol, timeframe, history, cancellationToken).ConfigureAwait(false);
            _candlesIngested.Add(history.Count, TagsFor(symbol, timeframe));

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "Backfilled {Count} candles for {Symbol}/{Timeframe} (from {From} to {To})",
                    history.Count,
                    symbol,
                    timeframe,
                    history[0].TsOpenUtc,
                    history[^1].TsOpenUtc);
            }
        }
        finally
        {
            _restGate.Release();
        }
    }

    private async Task PersistClosedCandleAsync(string symbol, Timeframe timeframe, Candle candle, CancellationToken cancellationToken)
    {
        await _writeRepository.UpsertAsync(symbol, timeframe, new[] { candle }, cancellationToken).ConfigureAwait(false);
        if (_strategyEngine is not null)
        {
            var closeTimestamp = candle.TsOpenUtc + timeframe.ToTimeSpan();
            await _strategyEngine.EvaluateAsync(symbol, timeframe, closeTimestamp, cancellationToken).ConfigureAwait(false);
        }
        var tags = TagsFor(symbol, timeframe);
        _candlesIngested.Add(1, tags);
        _ingestLatency.Record((DateTimeOffset.UtcNow - candle.TsOpenUtc).TotalMilliseconds, tags);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Persisted closed candle for {Symbol}/{Timeframe} @ {Ts}", symbol, timeframe, candle.TsOpenUtc);
        }
    }

    private async Task CacheInProgressAsync(string symbol, Timeframe timeframe, Candle candle)
    {
        if (_redis is null)
        {
            return;
        }

        var key = $"bybit:realtime:{symbol}:{timeframe.ToLabel()}";
        var payload = JsonSerializer.Serialize(new
        {
            symbol,
            timeframe = timeframe.ToLabel(),
            candle.TsOpenUtc,
            candle.Open,
            candle.High,
            candle.Low,
            candle.Close,
            candle.Volume
        });

        await _redis.StringSetAsync(key, payload, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
    }

    private KeyValuePair<string, object?>[] TagsFor(string symbol, Timeframe timeframe)
    {
        return _tagCache.GetOrAdd((symbol, timeframe), static key => new[]
        {
            new KeyValuePair<string, object?>("symbol", key.Symbol),
            new KeyValuePair<string, object?>("timeframe", key.Timeframe.ToLabel())
        });
    }
}







