using System.Text.Json;
using Microsoft.Extensions.Options;

namespace SmartTrader.Worker.Strategies;

public sealed class StrategyParameterProvider : IStrategyParameterProvider
{
    private readonly IOptionsMonitor<StrategyParametersOptions> _options;

    public StrategyParameterProvider(IOptionsMonitor<StrategyParametersOptions> options)
    {
        _options = options;
    }

    public JsonDocument GetParameters(string strategyName)
    {
        var snapshot = _options.CurrentValue;
        if (snapshot.Parameters.TryGetValue(strategyName, out var element))
        {
            return JsonDocument.Parse(element.GetRawText());
        }

        return JsonDocument.Parse("{}");
    }
}

