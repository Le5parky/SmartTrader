using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartTrader.Service.Interfaces;
using SmartTrader.Service.Objects;
using SmartTrader.Service.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new ConsoleMessage("Hello, World!"));
builder.Services.AddSingleton<MainBackgroundService>();
builder.Services.AddSingleton<IMainService>(sp => sp.GetRequiredService<MainBackgroundService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<MainBackgroundService>());

using var host = builder.Build();
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    lifetime.StopApplication();
    Console.WriteLine("Shutting down gracefully...");
};

await host.RunAsync();
