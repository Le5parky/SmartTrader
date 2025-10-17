using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Trading.Indicators.Calculators;

internal interface IIndicatorCalculator
{
    string Name { get; }

    int GetWarmupCandleCount(IndicatorRequest request);

    IndicatorResult Calculate(IndicatorRequest request, IReadOnlyList<Candle> candles);
}
