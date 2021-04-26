using Autofac;
using Microsoft.Extensions.Logging;
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
    }
  }
}