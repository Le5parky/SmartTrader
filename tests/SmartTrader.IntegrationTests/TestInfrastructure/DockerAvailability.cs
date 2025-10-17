using Docker.DotNet;
using System;
using System.Threading;

namespace SmartTrader.IntegrationTests.TestInfrastructure;

internal static class DockerAvailability
{
    private static readonly Lazy<bool> Availability = new(CheckAvailability, LazyThreadSafetyMode.ExecutionAndPublication);

    public static bool IsAvailable => Availability.Value;

    private static bool CheckAvailability()
    {
        try
        {
            using var client = new DockerClientConfiguration().CreateClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            client.System.PingAsync(cts.Token).GetAwaiter().GetResult();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

