using System;
namespace Common
{
  public interface ICounter
  {
    string NodeId { get; set; }
    int Count { get; set; }
    DateTime RecordTime { get; set; }
  }
}