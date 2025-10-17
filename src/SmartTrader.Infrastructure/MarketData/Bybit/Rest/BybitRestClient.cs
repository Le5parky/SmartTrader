using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.MarketData.Bybit.Internal;
using SmartTrader.Infrastructure.MarketData.Bybit.Options;
using SmartTrader.Trading.Abstractions.Models;

namespace SmartTrader.Infrastructure.MarketData.Bybit.Rest;

internal interface IBybitRestClient
{
    Task<IReadOnlyList<Candle>> GetKlinesAsync(
        string symbol,
        Timeframe timeframe,
        DateTime startUtc,
        DateTime endUtc,
        int limit,
        CancellationToken cancellationToken);
}

internal sealed class BybitRestClient : IBybitRestClient
{
    private static readonly Uri KlineEndpoint = new("/v5/market/kline", UriKind.Relative);

    private readonly HttpClient _httpClient;
    private readonly ILogger<BybitRestClient> _logger;
    private readonly BybitOptions _options;

    public BybitRestClient(HttpClient httpClient, IOptions<BybitOptions> options, ILogger<BybitRestClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<Candle>> GetKlinesAsync(
        string symbol,
        Timeframe timeframe,
        DateTime startUtc,
        DateTime endUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string>
        {
            ["category"] = _options.Category,
            ["symbol"] = symbol,
            ["interval"] = BybitTimeframeMapper.ToInterval(timeframe),
            ["start"] = new DateTimeOffset(startUtc).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            ["end"] = new DateTimeOffset(endUtc).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            ["limit"] = limit.ToString(CultureInfo.InvariantCulture)
        };

        var queryString = string.Join("&", query.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
        var requestUri = new Uri($"{KlineEndpoint}?{queryString}", UriKind.Relative);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BybitKlineResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        if (payload is null)
        {
            throw new InvalidOperationException("Bybit returned an empty payload for kline request.");
        }

        if (payload.RetCode != 0)
        {
            throw new InvalidOperationException($"Bybit returned non-success retCode {payload.RetCode}: {payload.RetMsg}");
        }

        var result = payload.Result;
        if (result is null)
        {
            return Array.Empty<Candle>();
        }

        var candles = new List<Candle>(result.Items.Count);
        foreach (var item in result.Items)
        {
            if (item.Start is null)
            {
                continue;
            }

            if (!long.TryParse(item.Start, NumberStyles.Integer, CultureInfo.InvariantCulture, out var startMs))
            {
                _logger.LogWarning("Failed to parse kline start '{start}' for {symbol} {timeframe}", item.Start, symbol, timeframe);
                continue;
            }

            var openTime = DateTimeOffset.FromUnixTimeMilliseconds(startMs);
            static decimal ParseDecimal(string? value) => decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

            candles.Add(new Candle(
                openTime,
                ParseDecimal(item.Open),
                ParseDecimal(item.High),
                ParseDecimal(item.Low),
                ParseDecimal(item.Close),
                ParseDecimal(item.Volume)));
        }

        candles.Sort(static (a, b) => a.TsOpenUtc.CompareTo(b.TsOpenUtc));
        return candles;
    }
}




