using System.Text.RegularExpressions;
using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Enums;
using System.Globalization;

namespace CryptoTrader.Domain.Parsers;

public class CatMassageParser : IMessageParser
{
    public Task<BaseOrder> ParseMessageAsync(string text)
    {
        // 🔴 ZECUSDT SHORT
        // 📍 Вход: 525.98
        // 🛑 SL: 530.88
        // 🎯 TP1: 522.53
        // 🎯 TP2: 519.12
        // 🎯 TP3: 517.82
        // 🎯 TP4: 517.31

        var symbolMatch = Regex.Match(text, @"(🔴|🟢)\s+([A-Z0-9]+)\s+(SHORT|LONG)");
        var entryMatch = Regex.Match(text, @"📍 Вход:\s+([\d.]+)");
        var slMatch = Regex.Match(text, @"🛑 SL:\s+([\d.]+)");
        var tpMatches = Regex.Matches(text, @"🎯 TP\d+:\s+([\d.]+)");

        if (!symbolMatch.Success || !entryMatch.Success || !slMatch.Success)
            throw new FormatException("Failed to parse signal message");

        var order = new BaseOrder
        {
            Side = symbolMatch.Groups[3].Value == "LONG" ? OrderSide.Buy : OrderSide.Sell,
            Symbol = symbolMatch.Groups[2].Value,
            InputPrice = decimal.Parse(entryMatch.Groups[1].Value, CultureInfo.InvariantCulture),
            StopLoss = decimal.Parse(slMatch.Groups[1].Value, CultureInfo.InvariantCulture)
        };

        foreach (Match tpMatch in tpMatches)
        {
            order.TakeProfits.Add(new TakeProfitSignal
            {
                Price = decimal.Parse(tpMatch.Groups[1].Value, CultureInfo.InvariantCulture)
            });
        }

        return Task.FromResult(order);
    }
}
