namespace SmartTrader.Trading.Strategies.Runtime;

public interface IStrategyCatalog
{
    Task<IReadOnlyCollection<StrategyDescriptor>> GetAllAsync(CancellationToken cancellationToken);

    Task<StrategyDescriptor?> TryGetAsync(string name, CancellationToken cancellationToken);

    Task ReloadAsync(CancellationToken cancellationToken);
}
