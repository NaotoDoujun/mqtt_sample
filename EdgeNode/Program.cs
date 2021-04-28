using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using MassTransit;
using NLog.Web;
using EdgeNode.Services;
using EdgeNode.Models;
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
            .ConfigureServices((hostContext, services) =>
            {
              // db
              services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=node.db"));

              // masstransit
              services.AddMassTransit(x =>
              {
                x.UsingRabbitMq((context, cfg) =>
                {
                  // configure health checks for this bus instance
                  cfg.UseHealthCheck(context);

                  cfg.Host("broker.local", h =>
                  {
                    h.Username("rabbitmq");
                    h.Password("rabbitmq");
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
              builder.Register(c => new MainService(
                c.Resolve<IBus>(),
                c.Resolve<IServiceScopeFactory>(),
                c.Resolve<ILogger<MainService>>()))
              .As<IHostedService>()
              .InstancePerLifetimeScope();
            })
            .ConfigureLogging(logging =>
            {
              logging.ClearProviders();
              logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            })
            .UseNLog();
  }
}
