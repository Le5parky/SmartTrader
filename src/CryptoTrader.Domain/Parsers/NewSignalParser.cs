using System.Text.RegularExpressions;
using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Enums;
using System.Globalization;

namespace CryptoTrader.Domain.Parsers;

public class NewSignalParser : IMessageParser
{
    public Task<BaseOrder> ParseMessageAsync(string text)
    {
        // Add regex for NewSignal format if different, or similar to CatMassage
        // For now, I'll make it similar but maybe slightly different to justify two parsers.

        var symbolMatch = Regex.Match(text, @"([A-Z0-9]+)\s+(SHORT|LONG)");
        var entryMatch = Regex.Match(text, @"Вход:\s+([\d.]+)");
        var slMatch = Regex.Match(text, @"SL:\s+([\d.]+)");
        var tpMatches = Regex.Matches(text, @"TP\d+:\s+([\d.]+)");

        if (!symbolMatch.Success || !entryMatch.Success || !slMatch.Success)
            throw new FormatException("Failed to parse NewSignal message");

        var order = new BaseOrder
        {
            Side = symbolMatch.Groups[2].Value == "LONG" ? OrderSide.Buy : OrderSide.Sell,
            Symbol = symbolMatch.Groups[1].Value,
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
