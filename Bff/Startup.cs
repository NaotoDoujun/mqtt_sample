using Autofac;
using System;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using MassTransit.Mqtt.MessageQueue.Serialisation;
using HotChocolate;
using HotChocolate.AspNetCore;
using Newtonsoft.Json;
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
                      .WithOrigins("https://localhost:3000", "https://localhost", "http://localhost:3000", "http://localhost")
                      .AllowAnyMethod()
                      .AllowAnyHeader()
              );
      });

      // masstransit
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
              .AddInMemorySubscriptions();
    }

    private void ConfigureMassTransitService(IServiceCollection services)
    {
      services.AddMassTransit(x =>
      {
        //x.AddConsumer<CounterConsumer>();

        x.UsingRabbitMq((context, cfg) =>
        {
          // configure health checks for this bus instance
          cfg.UseHealthCheck(context);

          cfg.Host("broker.local", h =>
          {
            h.Username("rabbitmq");
            h.Password("rabbitmq");
          });

          // just for MQTT
          cfg.ClearMessageDeserializers();
          var deserializer = JsonSerializer.CreateDefault();
          var jsonContent = new ContentType("application/json");
          cfg.AddMessageDeserializer(jsonContent, () => new RawJsonMessageDeserializer(jsonContent, deserializer));
          cfg.ReceiveEndpoint("masstransit.mqtt", e =>
          {
            e.Consumer(() => new MqttConsumer(context.GetService<IServiceScopeFactory>(), context.GetService<ILogger<MqttConsumer>>()));
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

      app.UseWebSockets();
      app.UseRouting();
      app.UseCors();
      app.UseEndpoints(endpoints =>
      {
        endpoints.MapGrpcService<GrpcService>();
        if (env.IsDevelopment()) endpoints.MapGrpcReflectionService();
        endpoints.MapGraphQL().WithOptions(
            new GraphQLServerOptions
            {
              Tool = { Enable = env.IsDevelopment() ? true : true }
            });
      });
    }
  }
}
