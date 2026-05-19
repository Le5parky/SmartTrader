using Microsoft.AspNetCore.Mvc;
using CryptoTrader.Application.Signals;

namespace CryptoTrader.Main.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class SignalsController : ControllerBase
{
    private readonly MonitoringQueue _queue;

    public SignalsController(MonitoringQueue queue)
    {
        _queue = queue;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveSignal([FromBody] SignalRequest request)
    {
        if (string.IsNullOrEmpty(request.Text))
        {
            return BadRequest("Signal text cannot be empty");
        }

        await _queue.EnqueueSignalAsync(request.Text);
        return Ok(new { Status = "Signal enqueued" });
    }
}

public class SignalRequest
{
    public string Text { get; set; } = string.Empty;
}
