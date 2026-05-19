using Autofac;
using Autofac.Extensions.DependencyInjection;
using CryptoTrader.DiExtensions.Autofac;
using CryptoTrader.Infrastructure.ByBit;
using CryptoTrader.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using CryptoTrader.Application.Interfaces;
using CryptoTrader.Application.Signals;

var builder = WebApplication.CreateBuilder(args);

// Configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// ASP.NET Core
builder.Services.AddControllers();

// Infrastructure
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddPooledDbContextFactory<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddDbContext<TradingDbContext>(options =>
    options.UseNpgsql(connectionString));

var useDemo = builder.Configuration.GetValue<bool>("ByBit:useDemo");
var apiKey = useDemo ? builder.Configuration["ByBit:apiKeyDemo"] : builder.Configuration["ByBit:apiKey"];
var apiSecret = useDemo ? builder.Configuration["ByBit:apiSecretDemo"] : builder.Configuration["ByBit:apiSecret"];
builder.Services.AddByBit(useDemo, apiKey ?? "", apiSecret ?? "");

// Autofac
builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule(new MainModule(builder.Configuration));
});

var app = builder.Build();

app.MapControllers();

app.Run();
