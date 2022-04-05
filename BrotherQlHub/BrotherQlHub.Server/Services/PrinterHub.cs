﻿using System.Collections.Concurrent;
using System.Reactive.Subjects;
using BrotherQlHub.Data;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BrotherQlHub.Server.Services;

public interface IPrinterClient
{
    
}

public class PrinterHub : Hub<IPrinterClient>
{
    private readonly ILogger<PrinterHub> _logger;
    private readonly SignalRPrinterClient _client;
    private readonly JsonSerializerSettings _jsonSettings;
    
    public PrinterHub(ILogger<PrinterHub> logger, SignalRPrinterClient client)
    {
        _logger = logger;
        _client = client;
        _jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            }
        };
    }

    public Task ReceivePrinterInfo(string payload)
    {
        _logger.LogInformation(payload);
        
        var message = JsonConvert.DeserializeObject<HostMessage>(payload, _jsonSettings);
        if (message is null)
        {
            _logger.LogError("Could not deserialize printer message {0}", payload);
            return Task.CompletedTask;
        }

        foreach (var info in message.Printers)
        {
            _client.OnNext(new PrinterUpdate(info, null, message.Host, message.Ip));
        }

        return Task.CompletedTask;
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