using Microsoft.Extensions.Options;

namespace SmartTrader.Trading.Strategies.Options;

internal sealed class StrategyPluginOptionsValidator : IValidateOptions<StrategyPluginOptions>
{
    public ValidateOptionsResult Validate(string? name, StrategyPluginOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.PluginsDirectory))
        {
            return ValidateOptionsResult.Fail("PluginsDirectory must be provided.");
        }

        return ValidateOptionsResult.Success;
    }
}
