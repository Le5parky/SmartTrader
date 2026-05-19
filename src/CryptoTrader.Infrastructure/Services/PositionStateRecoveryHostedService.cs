using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CryptoTrader.Domain.Interfaces;
using CryptoTrader.Application.Interfaces;
using Bybit.Net.Interfaces.Clients;
using Bybit.Net.Enums;
using Microsoft.EntityFrameworkCore;
using CryptoTrader.Infrastructure.Persistence;

namespace CryptoTrader.Infrastructure.Services;

public class PositionStateRecoveryHostedService : IHostedService
{
    private readonly IPositionStateRepository _repository;
    private readonly IBybitRestClient _restClient;
    private readonly IDealMonitoringService _monitoringService;
    private readonly IDbContextFactory<TradingDbContext> _dbContextFactory;
    private readonly ILogger<PositionStateRecoveryHostedService> _logger;

    public PositionStateRecoveryHostedService(
        IPositionStateRepository repository,
        IBybitRestClient restClient,
        IDealMonitoringService monitoringService,
        IDbContextFactory<TradingDbContext> dbContextFactory,
        ILogger<PositionStateRecoveryHostedService> logger)
    {
        _repository = repository;
        _restClient = restClient;
        _monitoringService = monitoringService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting state recovery...");

        // 1. Apply migrations
        using var context = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);

        // 2. Load open positions
        var openPositions = await _repository.GetOpenPositionsAsync();

        foreach (var pos in openPositions)
        {
            // 3a. Check if position exists on Bybit
            var bybitPos = await _restClient.V5Api.Trading.GetPositionsAsync(Category.Linear, pos.Symbol);
            if (!bybitPos.Success || !bybitPos.Data.List.Any(p => p.Symbol == pos.Symbol && p.Quantity > 0))
            {
                _logger.LogWarning("Position for {Symbol} not found on Bybit, closing in DB", pos.Symbol);
                pos.Close(DateTime.UtcNow);
                await _repository.UpdateAsync(pos);
                continue;
            }

            // 3g. Find missing TP-orders -> PlaceTakeProfitAsync (reinstallation)
            // For simplicity, we check active orders and compare with our TPs
            var activeOrders = await _restClient.V5Api.Trading.GetOrdersAsync(Category.Linear, pos.Symbol);
            if (activeOrders.Success)
            {
                foreach (var tp in pos.TakeProfits.Where(t => !t.IsTriggered))
                {
                    bool exists = activeOrders.Data.List.Any(o => o.Symbol == pos.Symbol && o.Price == tp.TargetPrice);
                    if (!exists)
                    {
                        _logger.LogInformation("Reinstalling missing TP for {Symbol} at {Price}", pos.Symbol, tp.TargetPrice);
                        await _restClient.V5Api.Trading.SetTradingStopAsync(
                            Category.Linear,
                            pos.Symbol,
                            PositionIdx.OneWayMode,
                            takeProfit: tp.TargetPrice,
                            takeProfitQuantity: tp.Quantity,
                            takeProfitOrderType: OrderType.Limit);
                    }
                }
            }

            // 3h. Restore monitoring
            await _monitoringService.MonitorDealForSymbol(pos.Symbol);
        }

        _logger.LogInformation("Recovery completed");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
