using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SmartTrader.Worker.Strategies;

public sealed class StrategyParametersOptions
{
    public Dictionary<string, JsonElement> Parameters { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

