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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
        }

        private Task MessageHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            var stringContents = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonConvert.DeserializeObject<HostMessage>(stringContents, _jsonSettings);

            foreach (var info in message.Printers)
            {
                UpdatePrinter(info);
            }

            return Task.CompletedTask;
        }

        private void GetTags()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetService<PrinterContext>();
            _printerTags = context.PrinterTags.ToList();
        }


        private void UpdatePrinter(PrinterInfo info)
        {
            if (_printerTags is null) GetTags();

            var tags = _printerTags.Where(t => t.PrinterSerial == info.Serial)
                .ToDictionary(p => p.TagCategoryId, p => p.TagId);

            var vm = new PrinterViewModel(info.Serial, true, DateTime.Now, info.Model, info.Status.Errors, 
                info.Status.MediaType, info.Status.MediaWidth, tags);

            _printers[info.Serial] = vm;
        }



    }
}