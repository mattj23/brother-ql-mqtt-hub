using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BrotherQlMqttHub.Data;
using BrotherQlMqttHub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Radzen;

namespace BrotherQlMqttHub
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
                builder.AddEventSourceLogger();
            });
            var logger = loggerFactory.CreateLogger("Startup");

            if (string.Equals(
                Environment.GetEnvironmentVariable("SSL_OFFLOAD"),
                "true", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("SSL offloading enabled, configuring header forwarding");
                services.Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                               ForwardedHeaders.XForwardedProto;
                    // Only loopback proxies are allowed by default.
                    // Clear that restriction because forwarders are enabled by explicit 
                    // configuration.
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });
            }

            var dbConnectionString = Configuration.GetConnectionString("Database");
            services.AddDbContext<PrinterContext>(options =>
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString)));
            services.AddSingleton<CategoryManager>();

            services.AddScoped<DialogService>();
            services.AddScoped<NotificationService>();
            services.AddControllers();
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddSingleton(x => new PrinterMonitor(Configuration.GetSection("Mqtt"), x.GetService<IServiceScopeFactory>()));
            services.AddHostedService(s => s.GetService<PrinterMonitor>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (string.Equals(
                Environment.GetEnvironmentVariable("SSL_OFFLOAD"),
                "true", StringComparison.OrdinalIgnoreCase))
            {
                app.UseForwardedHeaders();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.Use(next => context =>
            {
                Debug.WriteLine($"Found: {context.GetEndpoint()?.DisplayName}");
                return next(context);
            });


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}