namespace SmartTrader.Trading.Abstractions.Strategies;

public interface IStrategy
{
    string Name { get; }
    Version Version { get; }
    Task<StrategyResult> EvaluateAsync(StrategyContext context, CancellationToken cancellationToken);
}
