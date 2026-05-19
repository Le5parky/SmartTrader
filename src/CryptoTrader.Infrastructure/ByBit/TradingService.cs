using Bybit.Net.Interfaces.Clients;
using CryptoTrader.Application.Interfaces;
using CryptoTrader.Domain.Entities;
using CryptoTrader.Domain.Exceptions;
using CryptoTrader.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;
using CryptoExchange.Net.Objects;
using CryptoTrader.Infrastructure.Resilience;
using Polly;

namespace CryptoTrader.Infrastructure.ByBit;

public class TradingService : ITradingService
{
    private readonly IBybitRestClient _restClient;
    private readonly IPositionStateRepository _repository;
    private readonly IOrderService _orderService;
    private readonly ILogger<TradingService> _logger;
    private readonly ResiliencePipeline<WebCallResult<BybitOrderId>> _orderPipeline;
    private readonly ResiliencePipeline<WebCallResult> _tpPipeline;

    public TradingService(IBybitRestClient restClient, IPositionStateRepository repository, IOrderService orderService, ILogger<TradingService> logger)
    {
        _restClient = restClient;
        _repository = repository;
        _orderService = orderService;
        _logger = logger;
        _orderPipeline = ResiliencePipelines.CreateWritePipeline<WebCallResult<BybitOrderId>>();
        _tpPipeline = ResiliencePipelines.CreateWritePipeline<WebCallResult>();
    }

    public async Task<string> CreateOrderAsync(BaseOrder baseOrder)
    {
        // 1. ПЕРЕВІРКА ДУБЛІВ
        var existing = await _repository.GetOpenBySymbolAsync(baseOrder.Symbol);
        if (existing != null && (int)existing.Side == (int)baseOrder.Side)
        {
            throw new OrderValidationException($"Position for {baseOrder.Symbol} {baseOrder.Side} already exists.");
        }

        // 2. ОТРИМАННЯ ДАНИХ РАХУНКУ
        var balance = await _restClient.V5Api.Account.GetBalancesAsync(AccountType.Unified, "USDT");
        if (!balance.Success) throw new OrderCreationException("Failed to get balance");
        var usdtEquity = balance.Data.List.FirstOrDefault()?.TotalEquity ?? 0;

        // 3. ОТРИМАННЯ ПАРАМЕТРІВ СИМВОЛУ
        var instrument = await _restClient.V5Api.ExchangeData.GetLinearInverseSymbolsAsync(Category.Linear, baseOrder.Symbol);
        if (!instrument.Success) throw new OrderCreationException("Failed to get instrument info");
        var symbolInfo = instrument.Data.List.FirstOrDefault();
        if (symbolInfo == null) throw new OrderCreationException("Symbol info not found");
        var qtyStep = symbolInfo.LotSizeFilter.QuantityStep;

        // 4. РОЗРАХУНОК РОЗМІРУ ПОЗИЦІЇ
        // 1% USDT-маржі на позицію з плечем 10x
        decimal leverage = 10;
        decimal rawQty = (usdtEquity * 0.01m * leverage) / baseOrder.InputPrice;
        decimal quantity = Math.Floor(rawQty / qtyStep) * qtyStep;

        if (quantity <= 0) throw new OrderCreationException("Calculated quantity is zero or negative");

        // 5. РОЗМІЩЕННЯ ОСНОВНОГО ОРДЕРУ
        var order = await _orderPipeline.ExecuteAsync(async ct => await _restClient.V5Api.Trading.PlaceOrderAsync(
            Category.Linear,
            baseOrder.Symbol,
            baseOrder.Side == CryptoTrader.Domain.Enums.OrderSide.Buy ? Bybit.Net.Enums.OrderSide.Buy : Bybit.Net.Enums.OrderSide.Sell,
            NewOrderType.Limit,
            quantity,
            price: baseOrder.InputPrice,
            timeInForce: TimeInForce.GoodTillCanceled,
            isLeverage: true,
            positionIdx: PositionIdx.OneWayMode));

        if (!order.Success) throw new OrderCreationException($"Failed to place main order: {order.Error}");

        var mainOrderId = order.Data.OrderId;

        // 6. ЗБЕРЕЖЕННЯ СТАНУ
        var state = new OpenPositionState
        {
            Id = Guid.NewGuid(),
            Symbol = baseOrder.Symbol,
            MainOrderId = mainOrderId,
            Side = baseOrder.Side,
            EntryPrice = baseOrder.InputPrice,
            InitialStopLoss = baseOrder.StopLoss,
            CurrentStopLoss = baseOrder.StopLoss,
            TotalQuantity = quantity,
            PositionIdx = 0,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // 7. ОЧІКУВАННЯ ЗАПОВНЕННЯ
        bool filled = false;
        for (int i = 0; i < 120; i++)
        {
            var orderInfo = await _restClient.V5Api.Trading.GetOrdersAsync(Category.Linear, baseOrder.Symbol, orderId: mainOrderId);
            if (orderInfo.Success && orderInfo.Data.List.Any(o => o.Status == OrderStatus.Filled))
            {
                filled = true;
                break;
            }
            await Task.Delay(5000);
        }

        if (!filled) throw new OrderCreationException("Order was not filled in time");

        await _repository.AddAsync(state);

        // 8. РОЗМІЩЕННЯ ТЕЙК-ПРОФІТІВ
        var tpCount = baseOrder.TakeProfits.Count;
        decimal allocatedQty = 0;
        for (int i = 0; i < tpCount; i++)
        {
            var tpSignal = baseOrder.TakeProfits[i];
            decimal tpQty;
            if (i == tpCount - 1)
            {
                tpQty = quantity - allocatedQty; // Dust protection
            }
            else
            {
                decimal ratio = _orderService.GetTpRatio(tpCount, i + 1);
                tpQty = Math.Floor((quantity * ratio) / qtyStep) * qtyStep;
                allocatedQty += tpQty;
            }

            if (tpQty <= 0) continue;

            await _tpPipeline.ExecuteAsync(async ct => await _restClient.V5Api.Trading.SetTradingStopAsync(
                Category.Linear,
                baseOrder.Symbol,
                PositionIdx.OneWayMode,
                takeProfit: tpSignal.Price,
                takeProfitQuantity: tpQty,
                takeProfitOrderType: OrderType.Limit));

            state.TakeProfits.Add(new TakeProfitState
            {
                Sequence = i + 1,
                TargetPrice = tpSignal.Price,
                Quantity = tpQty,
                IsTriggered = false
            });
        }

        await _repository.UpdateAsync(state);

        return mainOrderId;
    }
}
