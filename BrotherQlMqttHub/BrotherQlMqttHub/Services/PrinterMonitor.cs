using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;

namespace BrotherQlMqttHub.Services
{
    public class PrinterMonitor : BackgroundService
    {
        private IMqttClient _client;
        private readonly IConfigurationSection _configSection;

        public PrinterMonitor(IConfigurationSection configSection)
        {
            _configSection = configSection;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_configSection["Host"], int.Parse(_configSection["Port"]))
                .WithClientId("brother_ql_hub")
                .Build();

            _client = new MqttFactory().CreateMqttClient();
            _client.UseApplicationMessageReceivedHandler(MessageHandler);
            _client.UseConnectedHandler(async e =>
            {
                // The connection occurs here
                await _client.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("label_servers/status/#").Build());
            });

            await _client.ConnectAsync(options, stoppingToken);
        }

        private Task MessageHandler(MqttApplicationMessageReceivedEventArgs e)
        {
            Debug.WriteLine(e.ApplicationMessage.Topic);

            return Task.CompletedTask;
        }
    }
}