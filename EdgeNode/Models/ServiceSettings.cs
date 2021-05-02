namespace EdgeNode
{
  public class ServiceSettings : IServiceSettings
  {
    public string NodeId { get; set; }
    public string ServiceType { get; set; }
    public int TimeSpan { get; set; }
  }
}