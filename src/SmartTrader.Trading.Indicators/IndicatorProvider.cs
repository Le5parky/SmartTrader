using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Indicators.Caching;
using SmartTrader.Trading.Indicators.Calculators;
using SmartTrader.Trading.Indicators.Options;

namespace SmartTrader.Trading.Indicators;

internal sealed class IndicatorProvider : IIndicatorProvider
{
    private readonly IReadOnlyDictionary<string, IIndicatorCalculator> _calculators;
    private readonly ICandleHistorySource _historySource;
    private readonly IIndicatorCache _cache;
    private readonly IndicatorCacheOptions _cacheOptions;
    private readonly ILogger<IndicatorProvider> _logger;

    public IndicatorProvider(
        IEnumerable<IIndicatorCalculator> calculators,
        ICandleHistorySource historySource,
        IIndicatorCache cache,
        IOptions<IndicatorCacheOptions> cacheOptions,
        ILogger<IndicatorProvider> logger)
    {
        _historySource = historySource;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
        _calculators = calculators.ToDictionary(c => c.Name, c => c, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IndicatorResult> GetAsync(IndicatorRequest request, CancellationToken cancellationToken)
    {
        var indicatorName = request.Name.Trim();
        if (!_calculators.TryGetValue(indicatorName, out var calculator))
        {
            throw new KeyNotFoundException($"Indicator '{indicatorName}' is not registered.");
        }

        var cached = await _cache.TryGetAsync(request, cancellationToken).ConfigureAwait(false);
        if (cached is not null)
        {
            return cached;
        }

        var requiredCandles = Math.Max(1, calculator.GetWarmupCandleCount(request));
        var history = await _historySource
            .GetHistoryAsync(request.Symbol, request.Timeframe, request.CandleTimestamp, requiredCandles, cancellationToken)
            .ConfigureAwait(false);

        if (history.Count < requiredCandles)
        {
            throw new InvalidOperationException(
                $"History provider returned {history.Count} candles but indicator '{request.Name}' requires {requiredCandles} for {request.Symbol}/{request.Timeframe} at {request.CandleTimestamp:O}.");
        }

        var result = calculator.Calculate(request, history);

        await _cache.SetAsync(request, result, _cacheOptions.RedisTtl, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Calculated indicator {Name} for {Symbol}/{Timeframe} @ {Timestamp}", request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp);
        }

        return result;
    }
}
