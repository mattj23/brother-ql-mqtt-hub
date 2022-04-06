using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using BrotherQlHub.Data;
using Microsoft.AspNetCore.SignalR;

namespace BrotherQlHub.Server.Services;

public class SignalRPrinterClient : IHostedService, IPrinterTransport, IObserver<Tuple<string, PrinterUpdate>>
{
    private readonly Subject<PrinterUpdate> _updates = new();
    private readonly ConcurrentDictionary<string, PrinterUpdate> _printers = new();
    private readonly ConcurrentDictionary<string, string> _contextIds = new();
    private readonly ILogger<SignalRPrinterClient> _logger;
    private IHubContext<PrinterHub, IPrinterClient>? _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    public SignalRPrinterClient(ILogger<SignalRPrinterClient> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        _hubContext = scope.ServiceProvider.GetService<IHubContext<PrinterHub, IPrinterClient>>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public IObservable<PrinterUpdate> Updates => _updates.AsObservable();
    
    public Task Print(string serial, string imageUrl)
    {
        var result = _contextIds.TryGetValue(serial, out var connId);
        if (!result) throw new KeyNotFoundException();
        
        _hubContext!.Clients.Client(connId!).SendPrintRequest(serial, 1, imageUrl);
        return Task.CompletedTask;
    }

    public Task Print(string serial, byte[] pngData)
    {
        var result = _contextIds.TryGetValue(serial, out var connId);
        if (!result) throw new KeyNotFoundException();
        
        _hubContext!.Clients.Client(connId!).SendPrintRequest(serial, 0, Convert.ToBase64String(pngData));
        return Task.CompletedTask;
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(Tuple<string, PrinterUpdate> value)
    {
        var update = new PrinterUpdate(value.Item2.Info, this, value.Item2.Host, value.Item2.Ip);
        _contextIds[value.Item2.Info.Serial] = value.Item1;
        _printers[value.Item2.Info.Serial] = update;
        _updates.OnNext(update);
    }
}