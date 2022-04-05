using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Security.Authentication;
using System.Text;
using BrotherQlHub.Data;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BrotherQlHub.Server.Services;

public class MqttPrinterClient: BackgroundService, IPrinterTransport
{
    private readonly Config _config;
    private IManagedMqttClient _client;
    private readonly JsonSerializerSettings _jsonSettings;
    private readonly ILogger<MqttPrinterClient> _logger;
    private readonly Subject<PrinterUpdate> _updates = new();
    private readonly ConcurrentDictionary<string, PrinterInfo> _printers = new();

    public MqttPrinterClient(Config config, ILogger<MqttPrinterClient> logger)
    {
        _config = config;
        _logger = logger;
        _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
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

    public async Task<bool> PrintRequest(string printerSerial, string imageUrl)
    {
        if (!_printers.ContainsKey(printerSerial)) return false;

        var printer = _printers[printerSerial];
        // TODO: Check if online

        var payload = new MqttApplicationMessageBuilder()
            .WithTopic($"label_servers/print/{printer.Host}/{printerSerial}/url")
            .WithPayload(imageUrl)
            .WithExactlyOnceQoS()
            .Build();
        await _client.PublishAsync(payload);
        return true;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
            .WithClientOptions(new MqttClientOptionsBuilder()
                .WithTcpServer(_config.Host, _config.Port)
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
        if (message is null)
        {
            _logger.LogError("Could not deserialize printer message {0}", stringContents);
            return Task.CompletedTask;
        }

        foreach (var info in message.Printers)
        {
            _printers[info.Serial] = info;
            var update = new PrinterUpdate(info, this);
            _updates.OnNext(update);
        }

        return Task.CompletedTask;
    }

    public class Config
    {
        public string Host { get; set; } = null!;
        public int Port { get; set; }

    }
}