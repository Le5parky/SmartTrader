namespace SmartTrader.Trading.Abstractions.Indicators;

public interface IIndicatorProvider
{
    Task<IndicatorResult> GetAsync(IndicatorRequest request, CancellationToken cancellationToken);
}
