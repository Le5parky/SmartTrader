using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CryptoTrader.Telegram.Services;
using CryptoTrader.Events;
using Telegram.Bot;

namespace CryptoTrader.Telegram.Services;

public class TelegramNotificationHostedService : IHostedService
{
    private readonly IEventAggregator _eventAggregator;
    private readonly ITelegramAlertService _alertService;
    private readonly ILogger<TelegramNotificationHostedService> _logger;
    private IDisposable? _subscription;

    public TelegramNotificationHostedService(
        IEventAggregator eventAggregator,
        ITelegramAlertService alertService,
        ILogger<TelegramNotificationHostedService> logger)
    {
        _eventAggregator = eventAggregator;
        _alertService = alertService;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _subscription = _eventAggregator.Subscribe<UpdateOrderEvent>(async e =>
        {
            try
            {
                if (e.IsCritical)
                {
                    await _alertService.SendCriticalAsync(e.Message);
                }
                else
                {
                    await _alertService.SendInfoAsync(e.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send telegram notification");
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _subscription?.Dispose();
        return Task.CompletedTask;
    }
}
