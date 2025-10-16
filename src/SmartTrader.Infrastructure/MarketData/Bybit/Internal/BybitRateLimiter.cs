using StackExchange.Redis;
using Microsoft.Extensions.Options;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;

namespace SmartTrader.Infrastructure.MarketData.Bybit.Internal;

internal interface IBybitRateLimiter
{
    Task WaitForSlotAsync(CancellationToken cancellationToken);
}

internal sealed class BybitRateLimiter : IBybitRateLimiter
{
    private const string AcquireScript = "local current = redis.call('GET', KEYS[1]) " +
        "if current and tonumber(current) >= tonumber(ARGV[1]) then return 0 end " +
        "current = redis.call('INCR', KEYS[1]) " +
        "if tonumber(current) == 1 then redis.call('PEXPIRE', KEYS[1], ARGV[2]) end " +
        "return current";

    private readonly IConnectionMultiplexer? _multiplexer;
    private readonly BybitRateLimitOptions _options;
    private readonly SemaphoreSlim _localLock = new(1, 1);
    private DateTime _localNextAllowed = DateTime.MinValue;

    public BybitRateLimiter(IConnectionMultiplexer? multiplexer, IOptions<BybitOptions> options)
    {
        _multiplexer = multiplexer;
        _options = options.Value.RateLimit;
    }

    public async Task WaitForSlotAsync(CancellationToken cancellationToken)
    {
        if (_multiplexer is null)
        {
            await WaitLocalAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        var db = _multiplexer.GetDatabase();
        var ttlMs = 60_000;
        const string keyPrefix = "bybit:rest:window:";

        while (!cancellationToken.IsCancellationRequested)
        {
            var window = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
            var key = (RedisKey)$"{keyPrefix}{window}";
            var result = (long)await db.ScriptEvaluateAsync(
                AcquireScript,
                new[] { key },
                new RedisValue[] { _options.RequestsPerMinute, ttlMs }).ConfigureAwait(false);

            if (result > 0)
            {
                return;
            }

            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WaitLocalAsync(CancellationToken cancellationToken)
    {
        await _localLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var now = DateTime.UtcNow;
            if (now < _localNextAllowed)
            {
                var delay = _localNextAllowed - now;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }

            var intervalMs = 60_000d / Math.Max(1, _options.RequestsPerMinute);
            _localNextAllowed = DateTime.UtcNow.AddMilliseconds(intervalMs);
        }
        finally
        {
            _localLock.Release();
        }
    }
}
