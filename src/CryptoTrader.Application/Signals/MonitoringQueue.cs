using System.Threading.Channels;
using CryptoTrader.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoTrader.Application.Signals;

public class MonitoringQueue : BackgroundService
{
    private readonly Channel<string> _channel;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoringQueue> _logger;
    private readonly SemaphoreSlim _semaphore = new(5);

    public MonitoringQueue(IServiceProvider serviceProvider, ILogger<MonitoringQueue> logger)
    {
        _channel = Channel.CreateUnbounded<string>();
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task EnqueueSignalAsync(string message)
    {
        await _channel.Writer.WriteAsync(message);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            _ = Task.Run(async () =>
            {
                await _semaphore.WaitAsync(stoppingToken);
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var traderService = scope.ServiceProvider.GetRequiredService<CryptoTraderService>();
                    await traderService.ProcessSignalAsync(message);
                }
                finally
                {
                    _semaphore.Release();
                }
            }, stoppingToken);
        }
    }
}
