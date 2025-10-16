using System.Buffers;
using System.Globalization;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.MarketData.Bybit.Internal;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;

namespace SmartTrader.Infrastructure.MarketData.Bybit.WebSocket;

internal interface IBybitWebSocketClient
{
    IAsyncEnumerable<CandleEvent> SubscribeKlinesAsync(
        string symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken);
}

internal sealed class BybitWebSocketClient : IBybitWebSocketClient
{
    private readonly BybitOptions _options;
    private readonly ILogger<BybitWebSocketClient> _logger;
    private readonly Random _random = new();

    public BybitWebSocketClient(IOptions<BybitOptions> options, ILogger<BybitWebSocketClient> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public IAsyncEnumerable<CandleEvent> SubscribeKlinesAsync(
        string symbol,
        Timeframe timeframe,
        CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<CandleEvent>(new BoundedChannelOptions(1_024)
        {
            SingleReader = true,
            SingleWriter = true,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        _ = Task.Run(() => RunAsync(symbol, timeframe, channel.Writer, cancellationToken), CancellationToken.None);

        return channel.Reader.ReadAllAsync(cancellationToken);
    }

    private async Task RunAsync(
        string symbol,
        Timeframe timeframe,
        ChannelWriter<CandleEvent> writer,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        var topic = $"kline.{BybitTimeframeMapper.ToInterval(timeframe)}.{symbol}";

        while (!cancellationToken.IsCancellationRequested)
        {
            using var socket = new ClientWebSocket();
            socket.Options.KeepAliveInterval = TimeSpan.FromMilliseconds(_options.Ws.HeartbeatMs);

            try
            {
                await socket.ConnectAsync(new Uri(_options.WsUrl), cancellationToken).ConfigureAwait(false);
                attempt = 0;
                _logger.LogInformation("Bybit WS connected for {symbol}/{timeframe}", symbol, timeframe);

                await SendSubscribeAsync(socket, topic, cancellationToken).ConfigureAwait(false);

                await ReceiveLoopAsync(socket, symbol, timeframe, writer, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Bybit WS connection failed for {symbol}/{timeframe}", symbol, timeframe);
            }

            var delayMs = Math.Min(
                _options.Ws.ReconnectMaxMs,
                (int)(_options.Ws.ReconnectBaseMs * Math.Pow(2, attempt++)) + _random.Next(0, 250));

            _logger.LogInformation(
                "Bybit WS reconnecting for {symbol}/{timeframe} in {delay} ms",
                symbol,
                timeframe,
                delayMs);

            try
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        writer.TryComplete();
    }

    private static async Task SendSubscribeAsync(ClientWebSocket socket, string topic, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(new
        {
            op = "subscribe",
            args = new[] { topic }
        });

        await socket.SendAsync(Encoding.UTF8.GetBytes(payload), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReceiveLoopAsync(
        ClientWebSocket socket,
        string symbol,
        Timeframe timeframe,
        ChannelWriter<CandleEvent> writer,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 8);
        try
        {
            while (!cancellationToken.IsCancellationRequested && socket.State == WebSocketState.Open)
            {
                var message = await ReceiveMessageAsync(socket, buffer, cancellationToken).ConfigureAwait(false);
                if (message is null)
                {
                    continue;
                }

                if (await TryHandleControlMessageAsync(socket, message, cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                try
                {
                    var envelope = JsonSerializer.Deserialize<BybitWsEnvelope>(message);
                    if (envelope?.Data is null || envelope.Topic is null)
                    {
                        continue;
                    }

                    foreach (var entry in envelope.Data)
                    {
                        if (entry.Start is null)
                        {
                            continue;
                        }

                        if (!long.TryParse(entry.Start, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startMs))
                        {
                            continue;
                        }

                        static decimal ParseDecimal(string? value) => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

                        var candle = new Candle(
                            DateTimeOffset.FromUnixTimeMilliseconds(startMs),
                            ParseDecimal(entry.Open),
                            ParseDecimal(entry.High),
                            ParseDecimal(entry.Low),
                            ParseDecimal(entry.Close),
                            ParseDecimal(entry.Volume));

                        var isClosed = entry.Confirm?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
                                       || entry.Confirm?.Equals("1", StringComparison.Ordinal) == true
                                       || entry.Confirm?.Equals("True", StringComparison.Ordinal) == true;

                        if (!writer.TryWrite(new CandleEvent(candle, isClosed)))
                        {
                            _logger.LogDebug("Dropping candle event for {symbol}/{timeframe} due to full channel", symbol, timeframe);
                        }
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Bybit WS payload: {payload}", message);
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<bool> TryHandleControlMessageAsync(ClientWebSocket socket, string message, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(message);
            if (document.RootElement.TryGetProperty("op", out var op))
            {
                var value = op.GetString();
                if (string.Equals(value, "ping", StringComparison.OrdinalIgnoreCase))
                {
                    var payload = Encoding.UTF8.GetBytes("{\"op\":\"pong\"}");
                    await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
                    return true;
                }
            }
        }
        catch (JsonException)
        {
            // ignore non-json control frames
        }

        return false;
    }

    private static async Task<string?> ReceiveMessageAsync(ClientWebSocket socket, byte[] buffer, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        WebSocketReceiveResult? result;
        do
        {
            result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken).ConfigureAwait(false);
                return null;
            }

            var chunk = Encoding.UTF8.GetString(buffer, 0, result.Count);
            builder.Append(chunk);
        }
        while (!result.EndOfMessage);

        return builder.ToString();
    }
}


