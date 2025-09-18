using System.Threading;
using System.Threading.Tasks;

namespace SmartTrader.Service.Interfaces;

public interface IMainService
{
    Task RunAsync(CancellationToken cancellationToken);
}
