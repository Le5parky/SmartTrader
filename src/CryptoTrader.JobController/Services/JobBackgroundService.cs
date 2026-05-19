using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CryptoTrader.JobController.Services;

public class JobBackgroundService : BackgroundService
{
    private readonly ILogger<JobBackgroundService> _logger;

    public JobBackgroundService(ILogger<JobBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("JobBackgroundService started (Placeholder)");
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
