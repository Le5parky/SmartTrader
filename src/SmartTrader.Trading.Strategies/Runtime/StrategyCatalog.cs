using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using SmartTrader.Trading.Strategies.Loading;

namespace SmartTrader.Trading.Strategies.Runtime;

public sealed class StrategyCatalog : IStrategyCatalog
{
    private readonly IStrategyPluginLoader _loader;
    private readonly ILogger<StrategyCatalog> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ConcurrentDictionary<string, StrategyDescriptor>? _cache;

    public StrategyCatalog(IStrategyPluginLoader loader, ILogger<StrategyCatalog> logger)
    {
        _loader = loader;
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<StrategyDescriptor>> GetAllAsync(CancellationToken cancellationToken)
    {
        var cache = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return cache.Values.ToList();
    }

    public async Task<StrategyDescriptor?> TryGetAsync(string name, CancellationToken cancellationToken)
    {
        var cache = await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return cache.TryGetValue(name, out var descriptor) ? descriptor : null;
    }

    public async Task ReloadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await LoadInternalAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ConcurrentDictionary<string, StrategyDescriptor>> EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cache is not null)
        {
            return _cache;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _cache ??= await LoadInternalAsync(cancellationToken).ConfigureAwait(false);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ConcurrentDictionary<string, StrategyDescriptor>> LoadInternalAsync(CancellationToken cancellationToken)
    {
        var descriptors = await _loader.LoadAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Loaded {Count} trading strategies.", descriptors.Count);
        return new ConcurrentDictionary<string, StrategyDescriptor>(
            descriptors.ToDictionary(d => d.Name, d => d, StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }
}
