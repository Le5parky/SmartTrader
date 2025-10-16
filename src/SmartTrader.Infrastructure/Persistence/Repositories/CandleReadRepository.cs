using Microsoft.EntityFrameworkCore;
using SmartTrader.Domain.MarketData;
using SmartTrader.Infrastructure.Persistence.Entities;

namespace SmartTrader.Infrastructure.Persistence.Repositories;

public sealed class CandleReadRepository : ICandleReadRepository
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public CandleReadRepository(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<DateTimeOffset?> GetLastCandleOpenAsync(string symbol, Timeframe timeframe, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var symbolId = await context.Set<Symbol>()
            .Where(s => s.Name == symbol)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (symbolId == Guid.Empty)
        {
            return null;
        }

        return timeframe switch
        {
            Timeframe.M1 => await QueryAsync<Candle1m>(context, symbolId, cancellationToken).ConfigureAwait(false),
            Timeframe.M5 => await QueryAsync<Candle5m>(context, symbolId, cancellationToken).ConfigureAwait(false),
            Timeframe.M15 => await QueryAsync<Candle15m>(context, symbolId, cancellationToken).ConfigureAwait(false),
            _ => throw new NotSupportedException($"Timeframe {timeframe} is not mapped to a candles table.")
        };
    }

    private static async Task<DateTimeOffset?> QueryAsync<TEntity>(AppDbContext context, Guid symbolId, CancellationToken cancellationToken)
        where TEntity : class
    {
        return await context.Set<TEntity>()
            .AsNoTracking()
            .Where(e => EF.Property<Guid>(e, nameof(Candle1m.SymbolId)) == symbolId)
            .OrderByDescending(e => EF.Property<DateTimeOffset>(e, nameof(Candle1m.TsOpen)))
            .Select(e => EF.Property<DateTimeOffset>(e, nameof(Candle1m.TsOpen)))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
