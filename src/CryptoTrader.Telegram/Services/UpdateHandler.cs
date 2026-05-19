using Telegram.Bot;
using Telegram.Bot.Types;
using CryptoTrader.Application.Signals;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CryptoTrader.Telegram.Services;

public class UpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly MonitoringQueue _queue;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UpdateHandler> _logger;

    public UpdateHandler(ITelegramBotClient botClient, MonitoringQueue queue, IConfiguration configuration, ILogger<UpdateHandler> logger)
    {
        _botClient = botClient;
        _queue = queue;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update)
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
            return;

        var privateChatId = _configuration.GetValue<long>("Telegram:privateChatId");

        if (message.Chat.Id != privateChatId)
        {
            _logger.LogWarning("Ignored message from unauthorized chat: {ChatId}", message.Chat.Id);
            return;
        }

        _logger.LogInformation("Received signal message from {ChatId}", message.Chat.Id);
        await _queue.EnqueueSignalAsync(messageText);
    }
}
