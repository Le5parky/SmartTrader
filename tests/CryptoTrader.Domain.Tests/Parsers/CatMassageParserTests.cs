using CryptoTrader.Domain.Parsers;
using CryptoTrader.Domain.Enums;
using Xunit;

namespace CryptoTrader.Domain.Tests.Parsers;

public class CatMassageParserTests
{
    [Fact]
    public async Task ParseMessageAsync_ValidShortSignal_ReturnsCorrectBaseOrder()
    {
        // Arrange
        var parser = new CatMassageParser();
        var signalText = @"📢 Новый сигнал
🔴 ZECUSDT SHORT
📍 Вход: 525.98
🛑 SL: 530.88
🎯 TP1: 522.53
🎯 TP2: 519.12
🎯 TP3: 517.82
🎯 TP4: 517.31
🧮 Риск на сделку 1% от депозита";

        // Act
        var result = await parser.ParseMessageAsync(signalText);

        // Assert
        Assert.Equal("ZECUSDT", result.Symbol);
        Assert.Equal(OrderSide.Sell, result.Side);
        Assert.Equal(525.98m, result.InputPrice);
        Assert.Equal(530.88m, result.StopLoss);
        Assert.Equal(4, result.TakeProfits.Count);
        Assert.Equal(522.53m, result.TakeProfits[0].Price);
        Assert.Equal(517.31m, result.TakeProfits[3].Price);
    }
}
