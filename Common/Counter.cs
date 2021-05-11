using System;
namespace Common
{
  public class Counter : ICounter
  {
    public int Id { get; set; }
    public string NodeId { get; set; }
    public int Count { get; set; }
    public DateTime RecordTime { get; set; }
  }
}