using Autofac;
using Autofac.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using NLog.Web;
using CommandLine;
using DeviceId;
using EdgeNode.Services;
using EdgeNode.Models;
using Common;

namespace EdgeNode
{
  public class Program
  {
    public static async Task Main(string[] args)
    {
      await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureHostConfiguration(configHost =>
            {
              configHost.AddEnvironmentVariables();
              configHost.AddCommandLine(args);
            })
            .ConfigureAppConfiguration((hostContext, config) =>
            {
              hostContext.HostingEnvironment.EnvironmentName = System.Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "production";
              config.SetBasePath(Directory.GetCurrentDirectory());
              config.AddJsonFile("appsettings.json");
              config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", optional: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
              var brokerSettings = hostContext.Configuration.GetSection("BrokerSettings").Get<BrokerSettings>();
              // db
              services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=node.db"));

              // masstransit
              services.AddMassTransit(x =>
              {
                x.UsingRabbitMq((context, cfg) =>
                {
                  // configure health checks for this bus instance
                  cfg.UseHealthCheck(context);

                  cfg.Host(brokerSettings.Host, h =>
                  {
                    h.Username(brokerSettings.Username);
                    h.Password(brokerSettings.Password);
                  });
                  cfg.ConfigureEndpoints(context);
                });
              });
              services.Configure<HealthCheckPublisherOptions>(options =>
              {
                options.Delay = TimeSpan.FromSeconds(2);
                options.Predicate = (check) => check.Tags.Contains("ready");
              });
              services.AddMassTransitHostedService();
            })
            .ConfigureContainer<ContainerBuilder>(builder =>
            {
              //build deviceId
              string deviceId = new DeviceIdBuilder()
                                    .AddMachineName()
                                    .AddMacAddress()
                                    .AddProcessorId()
                                    .AddMotherboardSerialNumber()
                                    .ToString();
              Parser.Default.ParseArguments<ArgOptions>(args).WithParsed<ArgOptions>(o =>
              {
                builder.RegisterModule(new ServiceModule()
                {
                  NodeId = deviceId,
                  ServiceType = o.ServiceType,
                  TimeSpan = o.TimeSpan
                });
              });
            })
            .ConfigureLogging(logging =>
            {
              logging.ClearProviders();
              logging.SetMinimumLevel(LogLevel.Trace);
            })
            .UseNLog();
  }
}
