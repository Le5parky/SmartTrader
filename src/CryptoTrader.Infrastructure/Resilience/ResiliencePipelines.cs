using Polly;
using Polly.Retry;
using CryptoExchange.Net.Objects;

namespace CryptoTrader.Infrastructure.Resilience;

public static class ResiliencePipelines
{
    public static ResiliencePipeline<T> CreateWritePipeline<T>()
    {
        return new ResiliencePipelineBuilder<T>()
            .AddRetry(new RetryStrategyOptions<T>
            {
                MaxRetryAttempts = 2,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromSeconds(2),
                ShouldHandle = new PredicateBuilder<T>().HandleResult(result =>
                {
                    if (result is WebCallResult res) return !res.Success;
                    return false;
                })
            })
            .Build();
    }
}
