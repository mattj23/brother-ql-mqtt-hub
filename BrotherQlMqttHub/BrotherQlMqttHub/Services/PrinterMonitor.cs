using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrotherQlMqttHub.Data;
using BrotherQlMqttHub.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Radzen;

namespace BrotherQlMqttHub.Services
{
    public class PrinterMonitor : BackgroundService
    {
        private IManagedMqttClient _client;
        private readonly IConfigurationSection _configSection;
        private readonly JsonSerializerSettings _jsonSettings;

        private readonly ConcurrentDictionary<string, PrinterViewModel> _printers;
        private readonly Subject<PrinterViewModel> _printerUpdates;
        private readonly IServiceScopeFactory _scopeFactory;
        private List<PrinterTag> _printerTags;

        public PrinterMonitor(IConfigurationSection configSection, IServiceScopeFactory scopeFactory)
        {
            _printerUpdates = new Subject<PrinterViewModel>();
            _printers = new ConcurrentDictionary<string, PrinterViewModel>();

            _configSection = configSection;
            _scopeFactory = scopeFactory;
            _jsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
        }

        public IObservable<PrinterViewModel> PrinterUpdates => _printerUpdates.AsObservable();

        public PrinterViewModel[] GetPrinters() => _printers.Values.ToArray();

        public async Task<bool> PrintRequest(string printerSerial, string imageUrl)
        {
            if (!_printers.ContainsKey(printerSerial))
            {
                return false;
            }

            var printer = _printers[printerSerial];
            if (!printer.IsOnline)
            {
                return false;
            }

            var payload = new MqttApplicationMessageBuilder()
                .WithTopic($"label_servers/print/{printer.Host}/{printerSerial}/url")
                .WithPayload(imageUrl)
                .WithExactlyOnceQoS()
                .Build();
            await _client.PublishAsync(payload);
            return true;
        }

        public async Task SetPrinterTag(string printerSerial, int selectedTag)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<PrinterContext>();

            var desiredTag = context.Tags.Include(t => t.Category)
                .FirstOrDefault(t => t.Id == selectedTag);

            if (desiredTag is null) return;

            var target = context.PrinterTags.FirstOrDefault(p =>
                p.TagCategoryId == desiredTag.Category.Id && p.PrinterSerial == printerSerial);

            if (target is not null)
            {
                context.PrinterTags.Remove(target);
            }

            await context.PrinterTags.AddAsync(new PrinterTag
            {
                PrinterSerial = printerSerial,
                TagCategoryId = desiredTag.Category.Id,
                TagId = desiredTag.Id
            });

            await context.SaveChangesAsync();

            GetTags();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await LoadPrinters();

            var options = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(_configSection["Host"], int.Parse(_configSection["Port"]))
                    .WithTls(new MqttClientOptionsBuilderTlsParameters 
                        {
                            UseTls = true,
                            SslProtocol = SslProtocols.Tls12
                        })
                    .WithClientId("brother_ql_hub")
                    .Build())
                .Build();

            _client = new MqttFactory().CreateManagedMqttClient();
            _client.UseApplicationMessageReceivedHandler(MessageHandler);
            _client.UseConnectedHandler(async e =>
            {
                Debug.WriteLine("Connection to MQTT broker established");
                // The connection occurs here
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("label_servers/status/#").Build());
            });

            Debug.WriteLine("Attempting connection to MQTT broker...");
            await _client.StartAsync(options);

            Observable.Interval(TimeSpan.FromSeconds(5))
                .Subscribe(_ => CheckForOffline());
        }

        /// <summary>
        /// Load the printer information from the database
        /// </summary>
        /// <returns></returns>
        private async Task LoadPrinters()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<PrinterContext>();
            _printerTags = await context.PrinterTags.ToListAsync();

            var tempPrinters = await context.Printers.ToListAsync();
            foreach (var p in tempPrinters)
            {
                var lastSeen = p.LastSeen.HasValue ? p.LastSeen.Value : default;
                var tags = _printerTags.Where(t => t.PrinterSerial == p.Serial)
                    .ToDictionary(x => x.TagCategoryId, x => x.TagId);
                _printers[p.Serial] = new PrinterViewModel(p.Serial, string.Empty, string.Empty, false, lastSeen, p.Model, 0, string.Empty, 0, tags);
            }
        }

        private Task MessageHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            var stringContents = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonConvert.DeserializeObject<HostMessage>(stringContents, _jsonSettings);

            foreach (var info in message.Printers)
            {
                UpdatePrinter(message.Host, message.Ip, info);
            }

            return Task.CompletedTask;
        }

        private void GetTags()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<PrinterContext>();
            _printerTags = context.PrinterTags.ToList();
        }

        private void CheckForOffline()
        {
            foreach (var p in _printers.Values)
            {
                if (p.IsOnline && DateTime.Now - p.LastSeen > TimeSpan.FromSeconds(20))
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetService<PrinterContext>();
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
                var context = scope.ServiceProvider.GetService<PrinterContext>();
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
}