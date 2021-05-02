using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using MassTransit.Monitoring.Health;
namespace EdgeNode.Services
{
  public class ServiceModule : Module
  {
    public string NodeId { get; set; }
    public string ServiceType { get; set; }
    public int TimeSpan { get; set; }
    protected override void Load(ContainerBuilder builder)
    {
      // register properties as serviceSettings
      builder.Register(c => new ServiceSettings
      {
        NodeId = NodeId,
        ServiceType = ServiceType,
        TimeSpan = TimeSpan
      }).As<IServiceSettings>();

      switch (ServiceType)
      {
        case "MQTT":
          // MQTT
          builder.Register(c => new MqttService(
            c.Resolve<IServiceScopeFactory>(),
            c.Resolve<ILogger<MqttService>>(),
            c.Resolve<IHostApplicationLifetime>(),
            c.Resolve<IConfiguration>(),
            c.Resolve<IServiceSettings>()))
          .As<IHostedService>()
          .InstancePerLifetimeScope();
          break;
        case "AMQP":
          // AMQP
          builder.Register(c => new AmqpService(
            c.Resolve<IBus>(),
            c.Resolve<IBusHealth>(),
            c.Resolve<IServiceScopeFactory>(),
            c.Resolve<ILogger<AmqpService>>(),
            c.Resolve<IHostApplicationLifetime>(),
            c.Resolve<IConfiguration>(),
            c.Resolve<IServiceSettings>()))
          .As<IHostedService>()
          .InstancePerLifetimeScope();
          break;
        default:
          // gRPC
          builder.Register(c => new GrpcService(
            c.Resolve<IServiceScopeFactory>(),
            c.Resolve<ILogger<GrpcService>>(),
            c.Resolve<IHostApplicationLifetime>(),
            c.Resolve<IConfiguration>(),
            c.Resolve<IHostEnvironment>(),
            c.Resolve<IServiceSettings>()))
          .As<IHostedService>()
          .InstancePerLifetimeScope();
          break;
      }
    }
  }
}