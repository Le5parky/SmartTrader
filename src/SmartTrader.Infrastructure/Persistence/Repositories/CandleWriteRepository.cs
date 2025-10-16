using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Npgsql;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public sealed class CandleWriteRepository : ICandleWriteRepository
{
    private const int BatchSize = 500;

    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<CandleWriteRepository> _logger;
    private readonly ConcurrentDictionary<string, Guid> _symbolCache = new(StringComparer.OrdinalIgnoreCase);

    public CandleWriteRepository(IDbContextFactory<AppDbContext> contextFactory, ILogger<CandleWriteRepository> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task UpsertAsync(string symbol, Timeframe timeframe, IReadOnlyList<Candle> candles, CancellationToken cancellationToken)
    {
        if (candles.Count == 0)
        {
            return;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var symbolId = await GetOrCreateSymbolAsync(context, symbol, cancellationToken).ConfigureAwait(false);

        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var dbTransaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var transaction = (NpgsqlTransaction)dbTransaction.GetDbTransaction();

        for (var offset = 0; offset < candles.Count; offset += BatchSize)
        {
            var slice = candles.Skip(offset).Take(BatchSize).ToList();
            if (slice.Count == 0)
            {
                continue;
            }

            await UpsertChunkAsync(connection, transaction, symbolId, timeframe, slice, cancellationToken).ConfigureAwait(false);
        }

        await dbTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Upserted {Count} candles for {Symbol}/{Timeframe}", candles.Count, symbol, timeframe);
        }
    }

    private async Task<Guid> GetOrCreateSymbolAsync(AppDbContext context, string symbol, CancellationToken cancellationToken)
    {
        if (_symbolCache.TryGetValue(symbol, out var cachedId))
        {
            return cachedId;
        }

        var set = context.Set<Symbol>();
        var existing = await set.AsNoTracking().FirstOrDefaultAsync(s => s.Name == symbol, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            _symbolCache[symbol] = existing.Id;
            return existing.Id;
        }

        var (baseAsset, quoteAsset) = SplitSymbol(symbol);
        var entity = new Symbol
        {
            Id = Guid.NewGuid(),
            Name = symbol,
            BaseAsset = baseAsset,
            QuoteAsset = quoteAsset,
            IsActive = true
        };

        await set.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _symbolCache[symbol] = entity.Id;
        return entity.Id;
    }

    private static (string Base, string Quote) SplitSymbol(string symbol)
    {
        var knownQuotes = new[] { "USDT", "USDC", "BTC", "ETH", "USD", "EUR" };
        foreach (var quote in knownQuotes)
        {
            if (symbol.EndsWith(quote, StringComparison.OrdinalIgnoreCase) && symbol.Length > quote.Length)
            {
                var baseAsset = symbol[..^quote.Length];
                return (baseAsset, quote);
            }
        }

        return (symbol, symbol);
    }

    private static string ResolveTable(Timeframe timeframe) => timeframe switch
    {
        Timeframe.M1 => "market.candles_1m",
        Timeframe.M5 => "market.candles_5m",
        Timeframe.M15 => "market.candles_15m",
        _ => throw new NotSupportedException($"Timeframe {timeframe} is not mapped to a candles table.")
    };

    private static async Task UpsertChunkAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid symbolId,
        Timeframe timeframe,
        IReadOnlyList<Candle> candles,
        CancellationToken cancellationToken)
    {
        var tableName = ResolveTable(timeframe);
        var commandText = new StringBuilder();
        commandText.Append($"INSERT INTO {tableName} (\"SymbolId\", \"ts_open\", \"Open\", \"High\", \"Low\", \"Close\", \"Volume\") VALUES ");

        var parameters = new List<NpgsqlParameter>(candles.Count * 7);
        var parameterIndex = 0;

        foreach (var candle in candles)
        {
            var nameSymbol = $"@p{parameterIndex++}";
            var nameTs = $"@p{parameterIndex++}";
            var nameOpen = $"@p{parameterIndex++}";
            var nameHigh = $"@p{parameterIndex++}";
            var nameLow = $"@p{parameterIndex++}";
            var nameClose = $"@p{parameterIndex++}";
            var nameVolume = $"@p{parameterIndex++}";

            commandText.Append($"({nameSymbol}, {nameTs}, {nameOpen}, {nameHigh}, {nameLow}, {nameClose}, {nameVolume}),");

            parameters.Add(new NpgsqlParameter(nameSymbol, symbolId));
            parameters.Add(new NpgsqlParameter(nameTs, candle.TsOpenUtc.UtcDateTime));
            parameters.Add(new NpgsqlParameter(nameOpen, candle.Open));
            parameters.Add(new NpgsqlParameter(nameHigh, candle.High));
            parameters.Add(new NpgsqlParameter(nameLow, candle.Low));
            parameters.Add(new NpgsqlParameter(nameClose, candle.Close));
            parameters.Add(new NpgsqlParameter(nameVolume, candle.Volume));
        }

        commandText.Length -= 1;
        commandText.Append(" ON CONFLICT (\"SymbolId\", \"ts_open\") DO UPDATE SET ");
        commandText.Append("\"Open\" = EXCLUDED.\"Open\", ");
        commandText.Append("\"High\" = EXCLUDED.\"High\", ");
        commandText.Append("\"Low\" = EXCLUDED.\"Low\", ");
        commandText.Append("\"Close\" = EXCLUDED.\"Close\", ");
        commandText.Append("\"Volume\" = EXCLUDED.\"Volume\";");

        await using var command = new NpgsqlCommand(commandText.ToString(), connection, transaction);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}






