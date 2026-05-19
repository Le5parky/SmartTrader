using CryptoTrader.Domain.Entities;

namespace CryptoTrader.Domain.Parsers;

public interface IMessageParser
{
    Task<BaseOrder> ParseMessageAsync(string text);
}
