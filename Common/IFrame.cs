using System;
namespace Common
{
  public interface IFrame
  {
    byte[] Image { get; set; }
    DateTime LocalRecordTime { get; set; }
    DateTime UtcRecordTime { get; set; }
  }
}