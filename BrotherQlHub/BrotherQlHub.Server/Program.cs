using BrotherQlHub.Data;
using BrotherQlHub.Server;
using BrotherQlHub.Server.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Startup Logger
using var loggerFactory = LoggerFactory.Create(b =>
{
    b.SetMinimumLevel(LogLevel.Information);
    b.AddConsole();
    b.AddEventSourceLogger();
});
var logger = loggerFactory.CreateLogger("Startup");

// Configure services
//========================================================================================================
// Cross origin requests allowed
builder.Services.AddCors(options => 
    options.AddPolicy("CorsPolicy", b =>
    {
        b.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
    }));

// Database and other services
builder.Services.UseDatabase(builder.Configuration.GetSection("Database"));
builder.Services.AddSingleton<CategoryManager>();

// Set up MQTT Transport, if specified in configuration
var mqttSection = builder.Configuration.GetSection("Mqtt");
if (mqttSection.Value is not null)
{
    var mqttConfig = mqttSection.Get<MqttPrinterClient.Config>();
    logger.LogInformation("Configuring MQTT transport to {0}:{1}", mqttConfig.Host, mqttConfig.Port);
    builder.Services.AddSingleton(mqttConfig);
    builder.Services.AddSingleton<MqttPrinterClient>();
    builder.Services.AddSingleton<IPrinterTransport>(x => x.GetRequiredService<MqttPrinterClient>());
    builder.Services.AddHostedService<MqttPrinterClient>(x => x.GetService<MqttPrinterClient>()!);
}

// SignalR Transport
builder.Services.AddSingleton<SignalRPrinterClient>();
builder.Services.AddSingleton<IPrinterTransport>(x => x.GetRequiredService<SignalRPrinterClient>());
builder.Services.AddHostedService<SignalRPrinterClient>(x => x.GetService<SignalRPrinterClient>()!);

// Printer monitor
builder.Services.AddSingleton<PrinterMonitor>();
builder.Services.AddHostedService<PrinterMonitor>(x => x.GetService<PrinterMonitor>()!);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddMudServices();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

// Build the application
//========================================================================================================
var app = builder.Build();
app.ApplyMigrations();

var pathBase = Environment.GetEnvironmentVariable("PATH_BASE");
if (!string.IsNullOrEmpty(pathBase))
{
    logger.LogInformation($"Using pathbase middleware: {pathBase}");
    app.UsePathBase(pathBase);
}

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

var logging = Environment.GetEnvironmentVariable("HTTP_LOGGING");
if (logging?.ToLower() == "true")
{
    logger.LogInformation("Using HTTP logging");
    app.UseHttpLogging();
}

var httpsRedirect = Environment.GetEnvironmentVariable("HTTPS_REDIRECT");
if (httpsRedirect?.ToLower() == "true")
{
    logger.LogInformation("Using HTTPS redirect");
    app.UseHttpsRedirection();
}

app.UseStaticFiles();
app.UseRouting();
app.UseCors("CorsPolicy");
app.MapHub<PrinterHub>("/printer");
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");

app.Run();