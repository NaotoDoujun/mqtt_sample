using System;
namespace EdgeNode.Models
{
  public class Counter : Common.ICounter
  {
    public int Id { get; set; }
    public string NodeId { get; set; }
    public int Count { get; set; }
    public DateTime RecordTime { get; set; }
  }
}