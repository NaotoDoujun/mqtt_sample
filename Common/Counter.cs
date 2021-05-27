using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace Common
{
  public class Counter : ICounter
  {
    public int Id { get; set; }
    public string NodeId { get; set; }
    public int Count { get; set; }
    public DateTime LocalRecordTime { get; set; }

    [NotMapped]
    public DateTime UtcRecordTime
    {
      get { return LocalRecordTime.ToUniversalTime(); }
      set { LocalRecordTime = value.ToLocalTime(); }
    }
  }
}