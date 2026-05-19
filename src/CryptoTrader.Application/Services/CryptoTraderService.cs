using CryptoTrader.Application.Interfaces;
using CryptoTrader.Domain.Parsers;
using Microsoft.Extensions.Logging;
using CryptoTrader.Events;

namespace CryptoTrader.Application.Services;

public class CryptoTraderService
{
    private readonly MessageParserProcessor _parserProcessor;
    private readonly ITradingService _tradingService;
    private readonly IDealMonitoringService _monitoringService;
    private readonly IEventAggregator _eventAggregator;
    private readonly ILogger<CryptoTraderService> _logger;

    public CryptoTraderService(
        MessageParserProcessor parserProcessor,
        ITradingService tradingService,
        IDealMonitoringService monitoringService,
        IEventAggregator eventAggregator,
        ILogger<CryptoTraderService> logger)
    {
        _parserProcessor = parserProcessor;
        _tradingService = tradingService;
        _monitoringService = monitoringService;
        _eventAggregator = eventAggregator;
        _logger = logger;
    }

    public async Task ProcessSignalAsync(string messageText)
    {
        try
        {
            var baseOrder = await _parserProcessor.ProcessAsync(messageText);
            _logger.LogInformation("Processing signal for {Symbol} {Side}", baseOrder.Symbol, baseOrder.Side);

            var orderId = await _tradingService.CreateOrderAsync(baseOrder);
            _logger.LogInformation("Order created: {OrderId}", orderId);

            await _monitoringService.MonitorDealForSymbol(baseOrder.Symbol);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing signal");
            _eventAggregator.Publish(new UpdateOrderEvent
            {
                Message = $"❌ Помилка обробки сигналу: {ex.Message}",
                IsCritical = true
            });
        }
    }
}
