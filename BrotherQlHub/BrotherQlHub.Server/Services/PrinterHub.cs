using Microsoft.AspNetCore.SignalR;

namespace BrotherQlHub.Server.Services;

public class PrinterHub : Hub
{
    private readonly ILogger<PrinterHub> _logger;

    public PrinterHub(ILogger<PrinterHub> logger)
    {
        _logger = logger;
    }

    public async Task ReceivePrinterInfo(string encoded)
    {
        _logger.LogInformation("Received: {0}", encoded);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Connected {0}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Disconnected {0}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}