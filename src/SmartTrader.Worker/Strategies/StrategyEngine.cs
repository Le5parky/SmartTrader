using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Strategies;
using SmartTrader.Trading.Strategies.Runtime;

namespace SmartTrader.Worker.Strategies;

public sealed class StrategyEngine
{
    private readonly IStrategyCatalog _catalog;
    private readonly ICandleHistorySource _historySource;
    private readonly IIndicatorProvider _indicatorProvider;
    private readonly IStrategyParameterProvider _parameterProvider;
    private readonly StrategyEngineOptions _options;
    private readonly ILogger<StrategyEngine> _logger;

    public StrategyEngine(
        IStrategyCatalog catalog,
        ICandleHistorySource historySource,
        IIndicatorProvider indicatorProvider,
        IStrategyParameterProvider parameterProvider,
        IOptions<StrategyEngineOptions> options,
        ILogger<StrategyEngine> logger)
    {
        _catalog = catalog;
        _historySource = historySource;
        _indicatorProvider = indicatorProvider;
        _parameterProvider = parameterProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task EvaluateAsync(string symbol, Timeframe timeframe, DateTimeOffset candleTimestamp, CancellationToken cancellationToken)
    {
        var strategies = await _catalog.GetAllAsync(cancellationToken).ConfigureAwait(false);
        if (strategies.Count == 0)
        {
            return;
        }

        var timeframeLabel = timeframe.ToLabel();
        var history = await _historySource
            .GetHistoryAsync(symbol, timeframeLabel, candleTimestamp, _options.ContextHistoryBars, cancellationToken)
            .ConfigureAwait(false);

        if (history.Count == 0)
        {
            _logger.LogDebug("No history available for {Symbol}/{Timeframe} at {Timestamp}", symbol, timeframeLabel, candleTimestamp);
            return;
        }

        foreach (var descriptor in strategies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var parameters = _parameterProvider.GetParameters(descriptor.Name);
            var context = new StrategyContext
            {
                Symbol = symbol,
                Timeframe = timeframeLabel,
                CandleTimestamp = candleTimestamp,
                History = history,
                Parameters = parameters,
                Indicators = _indicatorProvider
            };

            StrategyResult result;
            try
            {
                result = await descriptor.Instance.EvaluateAsync(context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Strategy {Strategy} failed for {Symbol}/{Timeframe} @{Timestamp}", descriptor.Name, symbol, timeframeLabel, candleTimestamp);
                continue;
            }

            using var snapshot = result.Snapshot;
            if (result.Action == TradeAction.None)
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace(
                        "Strategy {Strategy} returned no action for {Symbol}/{Timeframe} @{Timestamp}. snapshot={Snapshot}",
                        descriptor.Name,
                        symbol,
                        timeframeLabel,
                        candleTimestamp,
                        snapshot.RootElement.GetRawText());
                }

                continue;
            }

            _logger.LogInformation(
                "Strategy {Strategy} -> {Action} (confidence {Confidence:P0}) for {Symbol}/{Timeframe} @{Timestamp}. Reason: {Reason}. Snapshot: {Snapshot}",
                descriptor.Name,
                result.Action,
                result.Confidence,
                symbol,
                timeframeLabel,
                candleTimestamp,
                result.Reason,
                snapshot.RootElement.GetRawText());
        }
    }
}
