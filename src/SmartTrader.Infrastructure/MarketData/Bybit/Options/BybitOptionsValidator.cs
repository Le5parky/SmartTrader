using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;

namespace SmartTrader.Infrastructure.MarketData.Bybit.Options;

public sealed class BybitOptionsValidator : IValidateOptions<BybitOptions>
{
    public ValidateOptionsResult Validate(string? name, BybitOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            failures.Add("BaseUrl must be provided.");
        }

        if (string.IsNullOrWhiteSpace(options.WsUrl))
        {
            failures.Add("WsUrl must be provided.");
        }

        if (options.Symbols is null || options.Symbols.Length == 0)
        {
            failures.Add("At least one symbol must be configured.");
        }

        if (options.Timeframes is null || options.Timeframes.Length == 0)
        {
            failures.Add("At least one timeframe must be configured.");
        }
        else
        {
            foreach (var tf in options.Timeframes)
            {
                if (!TimeframeExtensions.TryParse(tf, out _))
                {
                    failures.Add($"Unsupported timeframe '{tf}'.");
                }
            }
        }

        if (options.Rest is null)
        {
            failures.Add("REST options block must be provided.");
        }
        else
        {
            if (options.Rest.PageSize is < 1 or > 1000)
            {
                failures.Add("REST.PageSize must be between 1 and 1000 (Bybit limit).");
            }

            if (options.Rest.TimeoutSec <= 0)
            {
                failures.Add("REST.TimeoutSec must be positive.");
            }

            if (options.Rest.BackfillDays < 0)
            {
                failures.Add("REST.BackfillDays cannot be negative.");
            }

            if (options.Rest.MaxConcurrency < 1)
            {
                failures.Add("REST.MaxConcurrency must be at least 1.");
            }
        }

        if (options.Ws is null)
        {
            failures.Add("WS options block must be provided.");
        }
        else
        {
            if (options.Ws.ReconnectBaseMs <= 0)
            {
                failures.Add("WS.ReconnectBaseMs must be positive.");
            }

            if (options.Ws.ReconnectMaxMs < options.Ws.ReconnectBaseMs)
            {
                failures.Add("WS.ReconnectMaxMs must be greater than or equal to WS.ReconnectBaseMs.");
            }
        }

        if (options.RateLimit is null)
        {
            failures.Add("RateLimit options block must be provided.");
        }
        else
        {
            if (options.RateLimit.RequestsPerMinute <= 0)
            {
                failures.Add("RateLimit.RequestsPerMinute must be positive.");
            }

            if (options.RateLimit.BurstSize <= 0)
            {
                failures.Add("RateLimit.BurstSize must be positive.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}


