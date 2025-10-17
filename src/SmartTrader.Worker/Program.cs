using System.IO;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using SmartTrader.Infrastructure;
using SmartTrader.Trading.Indicators.DependencyInjection;
using SmartTrader.Trading.Indicators.Options;
using SmartTrader.Trading.Strategies.DependencyInjection;
using SmartTrader.Worker.Strategies;
using SmartTrader.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Serilog
builder.Services.AddLogging();
var logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger, dispose: true);

// Infrastructure & trading services
builder.Services.AddInfrastructure(builder.Configuration);

var pluginPath = Path.Combine(
    AppContext.BaseDirectory,
    builder.Configuration.GetValue<string>("Trading:Strategies:PluginsPath") ?? "plugins");
Directory.CreateDirectory(pluginPath);

builder.Services.AddTradingIndicators();
builder.Services.Configure<IndicatorCacheOptions>(builder.Configuration.GetSection("Trading:Indicators:Cache"));

builder.Services.AddStrategyPlugins(options =>
{
    options.PluginsDirectory = pluginPath;
    var allowed = builder.Configuration.GetSection("Trading:Strategies:Allowed").Get<string[]>();
    if (allowed is { Length: > 0 })
    {
        options.AllowedStrategies = allowed;
    }

    options.RequireAssemblySignature = builder.Configuration.GetValue("Trading:Strategies:RequireSignature", false);
});

builder.Services.Configure<StrategyParametersOptions>(builder.Configuration.GetSection("Trading:Strategies:Parameters"));
builder.Services.Configure<StrategyEngineOptions>(builder.Configuration.GetSection("Trading:Strategies"));
builder.Services.AddSingleton<IStrategyParameterProvider, StrategyParameterProvider>();
builder.Services.AddSingleton<StrategyEngine>();

builder.Services.AddHostedService<BybitIngestionWorker>();

// OpenTelemetry (metrics + tracing)
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService(
            serviceName: "SmartTrader.Worker",
            serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString());
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Otlp:Endpoint"] ?? "http://localhost:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(builder.Configuration["OpenTelemetry:Otlp:Endpoint"] ?? "http://localhost:4317");
            });
    });

var host = builder.Build();
host.Run();

