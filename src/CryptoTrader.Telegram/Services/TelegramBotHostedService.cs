using Telegram.Bot;
using Telegram.Bot.Polling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CryptoTrader.Telegram.Services;

public class TelegramBotHostedService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(ITelegramBotClient botClient, IServiceProvider serviceProvider, ILogger<TelegramBotHostedService> logger)
    {
        _botClient = botClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram Bot...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<global::Telegram.Bot.Types.Enums.UpdateType>()
        };

        _botClient.StartReceiving(
            updateHandler: async (client, update, ct) =>
            {
                using var scope = _serviceProvider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<UpdateHandler>();
                await handler.HandleUpdateAsync(update);
            },
            errorHandler: (client, ex, ct) =>
            {
                _logger.LogError(ex, "Telegram polling error");
                return Task.CompletedTask;
            },
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
