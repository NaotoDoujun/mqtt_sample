using System;
namespace Common
{
  public interface ICounter
  {
    int Id { get; set; }
    string NodeId { get; set; }
    int Count { get; set; }
    DateTime RecordTime { get; set; }
  }
}