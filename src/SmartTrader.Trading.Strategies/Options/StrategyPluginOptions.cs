using System;

namespace SmartTrader.Trading.Strategies.Options;

public sealed class StrategyPluginOptions
{
    public string PluginsDirectory { get; set; } = "plugins";

    public string[] AllowedStrategies { get; set; } = Array.Empty<string>();

    public bool RequireAssemblySignature { get; set; }
}
