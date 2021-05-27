using System;
namespace Common
{
  public interface ICounter
  {
    string NodeId { get; set; }
    int Count { get; set; }
    DateTime LocalRecordTime { get; set; }
    DateTime UtcRecordTime { get; set; }
  }
}