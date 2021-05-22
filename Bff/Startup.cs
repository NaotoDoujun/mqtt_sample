using Autofac;
using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using HotChocolate;
using Common;
using Bff.Models;
using Bff.Services;
namespace Bff
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      // grpc
      services.AddGrpc();
      services.AddGrpcReflection();

      // dbcontext
      services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite("Data Source=bff.db"));

      // cors
      services.AddCors(options =>
      {
        options.AddDefaultPolicy(
                  builder => builder
                      .WithOrigins("https://localhost:3000", "http://localhost:3000")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
              );
      });

      // masstransit (AMQP)
      ConfigureMassTransitService(services);

      // graphql
      ConfigureGraphQLService(services);
    }

    private void ConfigureGraphQLService(IServiceCollection services)
    {
      services.AddGraphQLServer()
              .AddQueryType<Query>()
              .AddSubscriptionType<Subscription>()
              .AddType<Counter>()
              .AddFiltering()
              .AddSorting()
              .AddInMemorySubscriptions();
    }

    private void ConfigureMassTransitService(IServiceCollection services)
    {
      var brokerSettings = Configuration.GetSection("BrokerSettings").Get<BrokerSettings>();
      services.AddMassTransit(x =>
      {
        x.AddConsumer<CounterConsumer>();
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
    }

    public void ConfigureContainer(ContainerBuilder builder)
    {
      builder.RegisterModule(new ServiceModule());
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }

      // http2
      app.MapWhen(context => context.Request.Protocol.Contains("2"), a =>
      {
        a.UseRouting();
        a.UseEndpoints(endpoints => endpoints.MapGrpcService<GrpcService>());
      });

      // http1
      app.MapWhen(context => context.Request.Protocol.Contains("1"), a =>
      {
        a.UseWebSockets();
        a.UseRouting();
        a.UseCors();
        a.UseEndpoints(endpoints => endpoints.MapGraphQL());
      });
    }
  }
}
