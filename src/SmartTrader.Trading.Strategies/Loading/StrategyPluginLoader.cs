using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Trading.Abstractions.Strategies;
using SmartTrader.Trading.Strategies.Options;

namespace SmartTrader.Trading.Strategies.Loading;

public sealed class StrategyPluginLoader : IStrategyPluginLoader
{
    private readonly StrategyPluginOptions _options;
    private readonly ILogger<StrategyPluginLoader> _logger;

    public StrategyPluginLoader(IOptions<StrategyPluginOptions> options, ILogger<StrategyPluginLoader> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyCollection<StrategyDescriptor>> LoadAsync(CancellationToken cancellationToken)
    {
        var path = Path.GetFullPath(_options.PluginsDirectory);
        if (!Directory.Exists(path))
        {
            _logger.LogWarning("Strategies plugins directory {Directory} does not exist.", path);
            return Task.FromResult<IReadOnlyCollection<StrategyDescriptor>>(Array.Empty<StrategyDescriptor>());
        }

        var allowed = _options.AllowedStrategies.Length == 0
            ? null
            : new HashSet<string>(_options.AllowedStrategies, StringComparer.OrdinalIgnoreCase);

        var descriptors = new Dictionary<string, StrategyDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fullPath = Path.GetFullPath(file);
            var info = new FileInfo(fullPath);
            if (info.Length == 0)
            {
                _logger.LogWarning("Plugin {Plugin} is empty.", fullPath);
                continue;
            }

            var context = new StrategyPluginLoadContext(fullPath);

            try
            {
                var assembly = context.LoadFromAssemblyPath(fullPath);
                if (_options.RequireAssemblySignature && !IsStrongNamed(assembly))
                {
                    _logger.LogWarning("Skipping plugin {Plugin} because it is not strongly signed.", fullPath);
                    context.Unload();
                    continue;
                }

                var produced = false;
                foreach (var descriptor in CreateDescriptors(assembly, context, fullPath, allowed))
                {
                    produced = true;
                    if (descriptors.TryGetValue(descriptor.Name, out var existing))
                    {
                        if (descriptor.Version > existing.Version)
                        {
                            _logger.LogWarning(
                                "Replacing strategy {Name} version {OldVersion} from {OldSource} with version {NewVersion} from {NewSource}.",
                                descriptor.Name,
                                existing.Version,
                                existing.SourcePath,
                                descriptor.Version,
                                descriptor.SourcePath);
                            existing.LoadContext.Unload();
                            descriptors[descriptor.Name] = descriptor;
                        }
                        else
                        {
                            _logger.LogInformation(
                                "Skipping strategy {Name} version {Version} from {Source} because version {ExistingVersion} is already loaded.",
                                descriptor.Name,
                                descriptor.Version,
                                descriptor.SourcePath,
                                existing.Version);
                        }
                    }
                    else
                    {
                        descriptors.Add(descriptor.Name, descriptor);
                    }
                }

                if (!produced)
                {
                    context.Unload();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load strategy plugin {Plugin}", fullPath);
                context.Unload();
            }
        }

        return Task.FromResult((IReadOnlyCollection<StrategyDescriptor>)descriptors.Values.ToList());
    }

    private IEnumerable<StrategyDescriptor> CreateDescriptors(
        Assembly assembly,
        StrategyPluginLoadContext context,
        string path,
        HashSet<string>? allowed)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (!typeof(IStrategy).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                _logger.LogWarning("Strategy type {Type} in {Assembly} lacks a public parameterless constructor.", type.FullName, assembly.FullName);
                continue;
            }

            if (Activator.CreateInstance(type) is not IStrategy strategy)
            {
                continue;
            }

            if (allowed is not null && !allowed.Contains(strategy.Name))
            {
                _logger.LogInformation("Skipping strategy {Name} (not whitelisted).", strategy.Name);
                continue;
            }

            yield return new StrategyDescriptor(strategy.Name, strategy.Version, strategy, assembly, context, path);
        }
    }

    private static bool IsStrongNamed(Assembly assembly)
    {
        var token = assembly.GetName().GetPublicKeyToken();
        return token is { Length: > 0 };
    }
}

