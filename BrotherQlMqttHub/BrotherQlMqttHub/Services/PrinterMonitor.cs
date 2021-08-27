using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BrotherQlMqttHub.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BrotherQlMqttHub.Services
{
    public class PrinterMonitor : BackgroundService
    {
        private IManagedMqttClient _client;
        private readonly IConfigurationSection _configSection;
        private readonly JsonSerializerSettings _jsonSettings;


        public PrinterMonitor(IConfigurationSection configSection)
        {
            _configSection = configSection;
            _jsonSettings = new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            };
        }

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
            Debug.WriteLine(e.ApplicationMessage.Topic);

            var stringContents = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var message = JsonConvert.DeserializeObject<HostMessage>(stringContents, _jsonSettings);


            return Task.CompletedTask;
        }
    }
}