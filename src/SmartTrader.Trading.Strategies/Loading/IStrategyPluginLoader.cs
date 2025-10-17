namespace SmartTrader.Trading.Strategies.Loading;

public interface IStrategyPluginLoader
{
    Task<IReadOnlyCollection<StrategyDescriptor>> LoadAsync(CancellationToken cancellationToken);
}
