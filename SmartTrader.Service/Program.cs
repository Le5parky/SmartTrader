using System.Threading;
using Microsoft.Extensions.DependencyInjection;

using var mainCts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; 
    mainCts.Cancel();

    Console.WriteLine("Shutting down gracefully...");
    // Perform any necessary cleanup here before exiting the application.
    // After cleanup, you might want to exit the application by Environment.Exit(0);
};

var services = new ServiceCollection();
services.AddSingleton<IService, Startup>();

using var serviceProvider = services.BuildServiceProvider();

using var scope = serviceProvider.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<IService>();
service.Execute(mainCts.Token);

public interface IService
{
    void Execute(CancellationToken cancellationToken);
}

public class Startup : IService
{
    public void Execute(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        Console.WriteLine("Hello, World!");
    }
}
