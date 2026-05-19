using Telegram.Bot;
using Microsoft.Extensions.Configuration;

namespace CryptoTrader.Telegram.Services;

public interface ITelegramAlertService
{
    Task SendInfoAsync(string message);
    Task SendCriticalAsync(string message);
}

public class TelegramAlertService : ITelegramAlertService
{
    private readonly ITelegramBotClient _botClient;
    private readonly long _privateChatId;

    public TelegramAlertService(ITelegramBotClient botClient, IConfiguration configuration)
    {
        _botClient = botClient;
        _privateChatId = configuration.GetValue<long>("Telegram:privateChatId");
    }

    public async Task SendInfoAsync(string message)
    {
        await _botClient.SendMessage(_privateChatId, $"ℹ️ *CryptoTrader*\n\n{message}", parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Markdown);
    }

    public async Task SendCriticalAsync(string message)
    {
        await _botClient.SendMessage(_privateChatId, $"⚠️ *CryptoTrader ALERT*\n\n{message}", parseMode: global::Telegram.Bot.Types.Enums.ParseMode.Markdown);
    }
}
