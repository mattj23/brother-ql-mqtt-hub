using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using BrotherQlHub.Data;
using BrotherQlHub.Server.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BrotherQlHub.Server.Services;

public class PrinterMonitor : IHostedService
{
    private readonly ConcurrentDictionary<string, PrinterViewModel> _printers;
    private readonly ConcurrentDictionary<string, IPrinterTransport> _transport = new();
    private readonly Subject<PrinterViewModel> _printerUpdates;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterMonitor> _logger;
    private List<PrinterTag>? _printerTags;

    private readonly IDisposable[] _subscriptions;
    private IDisposable? _offlineCheck;

    public PrinterMonitor(IEnumerable<IPrinterTransport> transports, IServiceScopeFactory scopeFactory, ILogger<PrinterMonitor> logger)
    {
        _printerUpdates = new Subject<PrinterViewModel>();
        _printers = new ConcurrentDictionary<string, PrinterViewModel>();

        _scopeFactory = scopeFactory;
        _logger = logger;
        _subscriptions = transports.Select(t => t.Updates.Subscribe(ReceiveUpdate)).ToArray();
    }

    public IObservable<PrinterViewModel> PrinterUpdates => _printerUpdates.AsObservable();

    public PrinterViewModel[] GetPrinters()
    {
        return _printers.Values.ToArray();
    }

    public async Task Print(string serial, string url)
    {
        var result = _transport.TryGetValue(serial, out var transport);
        if (!result) throw new KeyNotFoundException("Printer not found");

        await transport!.Print(serial, url);
    }
    
    public async Task Print(string serial, byte[] bytes)
    {
        var result = _transport.TryGetValue(serial, out var transport);
        if (!result) throw new KeyNotFoundException("Printer not found");

        await transport!.Print(serial, bytes);
    }

    private void ReceiveUpdate(PrinterUpdate update)
    {
        _logger.LogDebug("Received printer update: {0}", update);
        
        if (_printerTags is null) GetTags();

        var tags = _printerTags!.Where(t => t.PrinterSerial == update.Info.Serial)
            .ToDictionary(p => p.TagCategoryId, p => p.TagId);

        var vm = new PrinterViewModel(update.Info.Serial, update.Host, update.Ip, true, DateTime.Now, update.Info.Model,
            update.Info.Status?.Errors ?? 0,
            update.Info.Status?.MediaType ?? "Unknown",
            update.Info.Status?.MediaWidth ?? 0, tags);

        // If this is a new printer being observed, we should save it to the database
        if (!_printers.ContainsKey(update.Info.Serial))
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            context!.Printers.Add(new Printer
            {
                LastSeen = DateTime.Now,
                Model = update.Info.Model,
                Serial = update.Info.Serial
            });
            context.SaveChanges();
        }

        _transport[update.Info.Serial] = update.Transport!;
        _printers[update.Info.Serial] = vm;

        _printerUpdates.OnNext(vm);
        
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _offlineCheck = Observable.Interval(TimeSpan.FromSeconds(5))
            .Subscribe(_ => CheckForOffline());
        await LoadPrinters();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
        _offlineCheck?.Dispose();
        return Task.CompletedTask;
    }
    
    public async Task SetPrinterTag(string printerSerial, int selectedTag)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetService<HubContext>();

        var desiredTag = context.Tags.Include(t => t.Category)
            .FirstOrDefault(t => t.Id == selectedTag);

        if (desiredTag is null) return;

        var target = context.PrinterTags.FirstOrDefault(p =>
            p.TagCategoryId == desiredTag.Category.Id && p.PrinterSerial == printerSerial);

        if (target is not null) context.PrinterTags.Remove(target);

        await context.PrinterTags.AddAsync(new PrinterTag
        {
            PrinterSerial = printerSerial,
            TagCategoryId = desiredTag.Category.Id,
            TagId = desiredTag.Id
        });

        await context.SaveChangesAsync();

        GetTags();
    }

    /// <summary>
    ///     Load the printer information from the database
    /// </summary>
    /// <returns></returns>
    private async Task LoadPrinters()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetService<HubContext>();
        _printerTags = await context.PrinterTags.ToListAsync();

        var tempPrinters = await context.Printers.ToListAsync();
        foreach (var p in tempPrinters)
        {
            var lastSeen = p.LastSeen.HasValue ? p.LastSeen.Value : default;
            var tags = _printerTags.Where(t => t.PrinterSerial == p.Serial)
                .ToDictionary(x => x.TagCategoryId, x => x.TagId);
            _printers[p.Serial] = new PrinterViewModel(p.Serial, string.Empty, string.Empty, false, lastSeen, p.Model,
                0, string.Empty, 0, tags);
        }
    }

    private void GetTags()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetService<HubContext>();
        _printerTags = context!.PrinterTags.ToList();
    }

    private void CheckForOffline()
    {
        foreach (var p in _printers.Values)
            if (p.IsOnline && DateTime.Now - p.LastSeen > TimeSpan.FromSeconds(20))
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetService<HubContext>();
                var target = context.Printers.FirstOrDefault(x => x.Serial == p.Serial);
                if (target is not null)
                {
                    target.LastSeen = p.LastSeen;
                    context.SaveChanges();
                }

                var tags = _printerTags.Where(t => t.PrinterSerial == p.Serial)
                    .ToDictionary(p => p.TagCategoryId, p => p.TagId);

                var vm = new PrinterViewModel(p.Serial, p.Host, p.HostIp, false, p.LastSeen, p.Model, p.Errors,
                    p.MediaType, p.MediaWidth, tags);

                _printers[vm.Serial] = vm;
                _printerUpdates.OnNext(vm);
            }
    }

}