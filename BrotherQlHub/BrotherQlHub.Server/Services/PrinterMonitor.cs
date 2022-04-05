using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Authentication;
using System.Text;
using BrotherQlHub.Data;
using BrotherQlHub.Server.ViewModels;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BrotherQlHub.Server.Services;

public class PrinterMonitor : IHostedService
{
    private readonly IConfigurationSection _configSection;
    private readonly JsonSerializerSettings _jsonSettings;

    private readonly ConcurrentDictionary<string, PrinterViewModel> _printers;
    private readonly Subject<PrinterViewModel> _printerUpdates;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterMonitor> _logger;
    private List<PrinterTag> _printerTags;

    private readonly IDisposable[] _subscriptions;

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

    private void ReceiveUpdate(PrinterUpdate update)
    {
        _logger.LogInformation("Received update: {0}", update);
        
        
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }
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
        _printerTags = context.PrinterTags.ToList();
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

                _printerUpdates.OnNext(vm);
            }
    }

    private void UpdatePrinter(string host, string hostIp, PrinterInfo info)
    {
        if (_printerTags is null) GetTags();

        var tags = _printerTags.Where(t => t.PrinterSerial == info.Serial)
            .ToDictionary(p => p.TagCategoryId, p => p.TagId);

        var vm = new PrinterViewModel(info.Serial, host, hostIp, true, DateTime.Now, info.Model,
            info.Status?.Errors ?? 0,
            info.Status?.MediaType ?? "Unknown",
            info.Status?.MediaWidth ?? 0, tags);

        // If this is a new printer being observed, we should save it to the database
        if (!_printers.ContainsKey(info.Serial))
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<HubContext>();
            context.Printers.Add(new Printer
            {
                LastSeen = DateTime.Now,
                Model = info.Model,
                Serial = info.Serial
            });
            context.SaveChanges();
        }

        _printers[info.Serial] = vm;

        _printerUpdates.OnNext(vm);
    }

}