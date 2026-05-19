using Bybit.Net.Interfaces.Clients;
using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using System.Collections.Concurrent;
using CryptoTrader.Application.Interfaces;
using CryptoTrader.Events;
using CryptoTrader.Infrastructure.Resilience;
using Polly;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Hosting;

namespace CryptoTrader.Infrastructure.ByBit;

public class DealMonitoringService : IDealMonitoringService, IHostedService
{
    private readonly IBybitSocketClient _socketClient;
    private readonly IBybitRestClient _restClient;
    private readonly IPositionStateRepository _repository;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<DealMonitoringService> _logger;
    private readonly ConcurrentDictionary<string, bool> _monitoredSymbols = new();
    private readonly ResiliencePipeline<WebCallResult> _tpPipeline;

    public DealMonitoringService(
        IBybitSocketClient socketClient,
        IBybitRestClient restClient,
        IPositionStateRepository repository,
        IEventAggregator eventAggregator,
        ILogger<DealMonitoringService> logger)
    {
        _socketClient = socketClient;
        _restClient = restClient;
        _repository = repository;
        _eventAggregator = eventAggregator;
        _logger = logger;
        _tpPipeline = ResiliencePipelines.CreateWritePipeline<WebCallResult>();
    }

    public async Task MonitorDealForSymbol(string symbol)
    {
        if (_monitoredSymbols.TryAdd(symbol, true))
        {
            _logger.LogInformation("Started monitoring for {Symbol}", symbol);
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting Global Monitoring via WebSocket...");
        var result = await _socketClient.V5PrivateApi.SubscribeToOrderUpdatesAsync(async data =>
        {
            foreach (var order in data.Data)
            {
                if (!_monitoredSymbols.ContainsKey(order.Symbol)) continue;

                await HandleOrderUpdateAsync(order);
            }
        }, ct);

        if (!result.Success)
        {
            _logger.LogError("Failed to subscribe to order updates: {Error}", result.Error);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task HandleOrderUpdateAsync(BybitOrderUpdate order)
    {
        var state = await _repository.GetOpenBySymbolAsync(order.Symbol);
        if (state == null) return;

        if (order.Status == OrderStatus.Filled)
        {
            if (order.StopOrderType == StopOrderType.StopLoss)
            {
                state.Close(DateTime.UtcNow);
                state.AccumulatedPnl += order.ClosedPnl ?? 0;
                await _repository.UpdateAsync(state);
                _monitoredSymbols.TryRemove(order.Symbol, out _);
                _logger.LogInformation("Closed by SL: {Symbol}, PNL: {Pnl}", order.Symbol, order.ClosedPnl);
                _eventAggregator.Publish(new UpdateOrderEvent
                {
                    Symbol = order.Symbol,
                    Message = $"🛑 Угода по {order.Symbol} закрилась по Stop Loss. PNL: {order.ClosedPnl} USDT",
                    IsCritical = true
                });
            }
            else if (order.StopOrderType == StopOrderType.PartialTakeProfit)
            {
                if (state.TryAdvanceStopLoss(order.TriggerPrice ?? 0, DateTime.UtcNow, out int seq, out decimal? newSL))
                {
                    state.AccumulatedPnl += order.ClosedPnl ?? 0;

                    if (newSL.HasValue)
                    {
                        await _tpPipeline.ExecuteAsync(async ct => await _restClient.V5Api.Trading.SetTradingStopAsync(
                            Category.Linear,
                            state.Symbol,
                            PositionIdx.OneWayMode,
                            stopLoss: newSL.Value));
                    }

                    await _repository.UpdateAsync(state);

                    if (state.IsClosed)
                    {
                        _monitoredSymbols.TryRemove(order.Symbol, out _);
                        _logger.LogInformation("All TPs hit for {Symbol}", order.Symbol);
                        _eventAggregator.Publish(new UpdateOrderEvent
                        {
                            Symbol = order.Symbol,
                            Message = $"✅ Позиція {order.Symbol} повністю закрита — всі тейк-профіти спрацювали 🎉",
                            IsCritical = false
                        });
                    }
                    else
                    {
                        _logger.LogInformation("TP {Seq} hit for {Symbol}, New SL: {NewSL}", seq, order.Symbol, newSL);
                        _eventAggregator.Publish(new UpdateOrderEvent
                        {
                            Symbol = order.Symbol,
                            Message = $"🎯 {order.Symbol} спрацював TP ({seq}). SL: {newSL}. PNL: {order.ClosedPnl} USDT",
                            IsCritical = false
                        });
                    }
                }
            }
        }
        else if (order.Status == OrderStatus.Cancelled)
        {
            state.Close(DateTime.UtcNow);
            await _repository.UpdateAsync(state);
            _monitoredSymbols.TryRemove(order.Symbol, out _);
            _logger.LogInformation("Order cancelled: {Symbol}, Reason: {CancelType}", order.Symbol, order.CancelType);
            _eventAggregator.Publish(new UpdateOrderEvent
            {
                Symbol = order.Symbol,
                Message = $"❌ Ордер по {order.Symbol} відмінено. Причина: {order.CancelType}",
                IsCritical = true
            });
        }
    }
}
