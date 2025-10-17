using Xunit;

namespace SmartTrader.IntegrationTests.TestInfrastructure;

public sealed class RequiresDockerFactAttribute : FactAttribute
{
    public RequiresDockerFactAttribute()
    {
        if (!DockerAvailability.IsAvailable)
        {
            Skip = "Docker is not available. Skipping containerized integration tests.";
        }
    }
}
