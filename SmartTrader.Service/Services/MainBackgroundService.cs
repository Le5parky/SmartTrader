using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using SmartTrader.Service.Interfaces;
using SmartTrader.Service.Objects;

namespace SmartTrader.Service.Services;

public class MainBackgroundService : BackgroundService, IMainService
{
    private readonly ConsoleMessage _message;

    public MainBackgroundService(ConsoleMessage message)
    {
        _message = message;
    }

    public Task RunAsync(CancellationToken cancellationToken)
    {
        return ExecuteCoreAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return ExecuteCoreAsync(stoppingToken);
    }

    private Task ExecuteCoreAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        Console.WriteLine(_message.Text);
        return Task.CompletedTask;
    }
}
