using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using BrotherQlHub.Data;

namespace BrotherQlHub.Server.Services;

public class SignalRPrinterClient : IHostedService, IPrinterTransport, IObserver<PrinterUpdate>
{
    private readonly Subject<PrinterUpdate> _updates = new();
    private readonly ConcurrentDictionary<string, PrinterUpdate> _printers = new();
    private readonly ILogger<SignalRPrinterClient> _logger;

    public SignalRPrinterClient(ILogger<SignalRPrinterClient> logger)
    {
        _logger = logger;
        
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public IObservable<PrinterUpdate> Updates => _updates.AsObservable();
    public Task<bool> Print(string serial, string imageUrl)
    {
        throw new NotImplementedException();
    }

    public Task<bool> Print(string serial, byte[] pngData)
    {
        throw new NotImplementedException();
    }

    public void OnCompleted()
    {
    }

    public void OnError(Exception error)
    {
    }

    public void OnNext(PrinterUpdate value)
    {
        var update = new PrinterUpdate(value.Info, this, value.Host, value.Ip);
        _printers[value.Info.Serial] = update;
        _updates.OnNext(update);
    }
}