using System.Reflection;
using System.Runtime.Loader;
using SmartTrader.Trading.Abstractions.Strategies;

namespace SmartTrader.Trading.Strategies;

public sealed record StrategyDescriptor(
    string Name,
    Version Version,
    IStrategy Instance,
    Assembly Assembly,
    AssemblyLoadContext LoadContext,
    string SourcePath);
