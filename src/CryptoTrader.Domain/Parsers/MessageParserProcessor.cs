using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Exceptions;

namespace CryptoTrader.Domain.Parsers;

public class MessageParserProcessor
{
    private readonly IEnumerable<IMessageParser> _parsers;

    public MessageParserProcessor(IEnumerable<IMessageParser> parsers)
    {
        _parsers = parsers;
    }

    public async Task<BaseOrder> ProcessAsync(string text)
    {
        foreach (var parser in _parsers)
        {
            try
            {
                return await parser.ParseMessageAsync(text);
            }
            catch (FormatException)
            {
                continue;
            }
        }

        throw new ParserNotFoundException("No suitable parser found for the signal");
    }
}
