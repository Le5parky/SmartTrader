using System.Text.Json;

namespace SmartTrader.Worker.Strategies;

public interface IStrategyParameterProvider
{
    JsonDocument GetParameters(string strategyName);
}
