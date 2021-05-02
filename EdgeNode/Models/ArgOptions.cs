using System;
using CommandLine;
namespace EdgeNode.Models
{
  public class ArgOptions
  {
    [Option('s', "servicetype", Required = false, HelpText = "Set serviceType. GRPC(default), ARQP, MQTT")]
    public string ServiceType { get; set; } = "GRPC";

    [Option('t', "timespan", Required = false, HelpText = "Set TimeSpan")]
    public int TimeSpan { get; set; } = 1000;
  }
}