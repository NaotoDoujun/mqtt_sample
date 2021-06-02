using System;
using System.ComponentModel.DataAnnotations.Schema;
namespace Common
{
  public class Frame : IFrame
  {
    public int Id { get; set; }
    public byte[] Image { get; set; }
    public DateTime LocalRecordTime { get; set; }

    [NotMapped]
    public DateTime UtcRecordTime
    {
      get { return LocalRecordTime.ToUniversalTime(); }
      set { LocalRecordTime = value.ToLocalTime(); }
    }
  }
}