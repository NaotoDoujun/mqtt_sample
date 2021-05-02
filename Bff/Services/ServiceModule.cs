using Autofac;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Bff.Services
{
  public class ServiceModule : Module
  {
    protected override void Load(ContainerBuilder builder)
    {
      builder.Register(c => new GrpcService(
        c.Resolve<IServiceScopeFactory>(),
        c.Resolve<ILogger<GrpcService>>()))
          .InstancePerLifetimeScope();

      builder.Register(c => new MqttService(
        c.Resolve<IServiceScopeFactory>(),
        c.Resolve<ILogger<MqttService>>(),
        c.Resolve<IHostApplicationLifetime>(),
        c.Resolve<IConfiguration>()))
        .As<IHostedService>()
        .InstancePerLifetimeScope();
    }
  }
}