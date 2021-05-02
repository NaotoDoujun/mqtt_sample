namespace EdgeNode
{
  public interface IServiceSettings
  {
    string NodeId { get; set; }
    string ServiceType { get; set; }
    int TimeSpan { get; set; }
  }
}