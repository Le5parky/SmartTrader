namespace CryptoTrader.Application.Interfaces;

public interface IDealMonitoringService
{
    Task MonitorDealForSymbol(string symbol);
}
