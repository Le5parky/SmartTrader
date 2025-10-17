using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Trading.Abstractions.Indicators;
using SmartTrader.Trading.Indicators.Options;
using StackExchange.Redis;

namespace SmartTrader.Trading.Indicators.Caching;

internal sealed class IndicatorCache : IIndicatorCache
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase? _redis;
    private readonly IndicatorCacheOptions _options;
    private readonly ILogger<IndicatorCache> _logger;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _memoryIndex = new(StringComparer.OrdinalIgnoreCase);

    public IndicatorCache(
        IMemoryCache memoryCache,
        IOptions<IndicatorCacheOptions> options,
        ILogger<IndicatorCache> logger,
        IDatabase? redis = null)
    {
        _memoryCache = memoryCache;
        _options = options.Value;
        _logger = logger;
        _redis = redis;
    }

    public async Task<IndicatorResult?> TryGetAsync(IndicatorRequest request, CancellationToken cancellationToken)
    {
        var key = IndicatorCacheKey.From(request);
        if (_memoryCache.TryGetValue(key.Value, out var cachedObj) && cachedObj is IndicatorResult cached)
        {
            _logger.LogTrace("Indicator cache hit (memory) for {Key}", key.Value);
            return cached;
        }

        if (_redis is null)
        {
            return null;
        }

        var redisKey = BuildRedisKey(key.Value);
        try
        {
            var payload = await _redis.StringGetAsync(redisKey).ConfigureAwait(false);
            if (!payload.HasValue)
            {
                return null;
            }

            var serialized = payload.ToString();
            if (serialized is null)
            {
                return null;
            }

            var result = Deserialize(request, serialized);
            IndexMemory(key, result);
            _logger.LogTrace("Indicator cache hit (redis) for {Key}", key.Value);
            return result;
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to read indicator cache from Redis for {Key}", redisKey);
            return null;
        }
    }

    public async Task SetAsync(IndicatorRequest request, IndicatorResult result, TimeSpan ttl, CancellationToken cancellationToken)
    {
        var key = IndicatorCacheKey.From(request);
        IndexMemory(key, result);

        if (_redis is null)
        {
            return;
        }

        var redisKey = BuildRedisKey(key.Value);
        try
        {
            var serialized = Serialize(result);
            var tasks = new List<Task>
            {
                _redis.StringSetAsync(redisKey, serialized, ttl)
            };

            var indexKey = BuildIndexKey(key.BaseKey);
            tasks.Add(_redis.SetAddAsync(indexKey, redisKey));
            tasks.Add(_redis.KeyExpireAsync(indexKey, ttl));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to write indicator cache to Redis for {Key}", redisKey);
        }
    }

    public async Task InvalidateAsync(string symbol, string timeframe, DateTimeOffset candleTimestamp, CancellationToken cancellationToken)
    {
        var baseKey = IndicatorCacheKey.BuildBaseKey(symbol, timeframe, candleTimestamp);

        if (_memoryIndex.TryRemove(baseKey, out var keys))
        {
            foreach (var item in keys.Keys)
            {
                _memoryCache.Remove(item);
            }
        }

        if (_redis is null)
        {
            return;
        }

        var indexKey = BuildIndexKey(baseKey);
        try
        {
            var members = await _redis.SetMembersAsync(indexKey).ConfigureAwait(false);
            if (members.Length > 0)
            {
                var keysToRemove = members.Select(static m => (RedisKey)m.ToString()).ToArray();
                if (keysToRemove.Length > 0)
                {
                    await _redis.KeyDeleteAsync(keysToRemove).ConfigureAwait(false);
                }
            }

            await _redis.KeyDeleteAsync(indexKey).ConfigureAwait(false);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate indicator cache for {BaseKey}", baseKey);
        }
    }

    private void IndexMemory(IndicatorCacheKey key, IndicatorResult result)
    {
        _memoryCache.Set(key.Value, result, _options.MemoryTtl);

        var group = _memoryIndex.GetOrAdd(key.BaseKey, _ => new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase));
        group[key.Value] = 0;
    }

    private string BuildRedisKey(string cacheKey) => $"{_options.KeyPrefix}:{cacheKey}";

    private string BuildIndexKey(string baseKey) => $"{_options.IndexPrefix}:{baseKey}";

    private static string Serialize(IndicatorResult result)
    {
        return JsonSerializer.Serialize(result.Values);
    }

    private static IndicatorResult Deserialize(IndicatorRequest request, string payload)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, decimal>>(payload);
        if (dictionary is null)
        {
            throw new InvalidOperationException("Cached indicator payload is invalid.");
        }

        return new IndicatorResult(request.Name, request.Symbol, request.Timeframe, request.CandleTimestamp, dictionary);
    }
}
