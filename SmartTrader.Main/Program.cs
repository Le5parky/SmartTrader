using System.ComponentModel;
using System.Collections.Immutable;
using System.Threading;
using Autofac;

using var mainCts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; 
    mainCts.Cancel();

    Console.WriteLine("Shutting down gracefully...");
    // Perform any necessary cleanup here before exiting the application.
    // After cleanup, you might want to exit the application by Environment.Exit(0);
};

var builder = new ContainerBuilder();
builder.RegisterType<Startup>().As<IService>();

using var container = builder.Build();


using (var scope = container.BeginLifetimeScope())
{
    var service = scope.Resolve<IService>();
    service.Execute(mainCts.Token);
}

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
