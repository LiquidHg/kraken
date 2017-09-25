using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kraken.SharePoint.Client {

  public interface ITransferTimeEstimator {

    //double EstimateUploadTime(double fileKiloBytes = 0);

    double FileKB { get; set; }
    ulong FileBytes { get; set; }

    ulong EstimatedTicks { get; }
    double EstimatedSeconds { get; set; }
    double EstimatedMinutes { get; set; }

    TimeSpan EstimatedTime { get; set; }
    TimeSpan ActualTime { get; set; }

    ulong TimeOutTicks { get; }


  }

  public class SimpleTransferTimeEstimator : ITransferTimeEstimator {

    /// <summary>
    /// A multiplier used to increase estimates for purposed of
    /// calculating timeouts and time to complete virus scans.
    /// </summary>
    /// <remarks>
    /// 1.25 was good for a while, but when things get slow it causes significant timeouts
    /// </remarks>
    public double HugeFileTimeOutMultiplier { get; set; }  = 1.5;

    /// <summary>
    /// A typical number of KB/second on O365; used to calculate timeouts
    /// </summary>
    public ulong TypicalBytesPerSecond { get; set; } = 409600; // about .4 MB/sec

    public double FileKB {
      get {
        return FileBytes / 1024;
      }
      set {
        this.FileBytes = (ulong)value * 1024;
      }
    }

    private ulong _bytes = 0;
    public ulong FileBytes {
      get {
        return _bytes;
      }
      set {
        _bytes = value;
        this.EstimatedTicks = _bytes * TypicalBytesPerSecond * 1000; // ticks
      }
    }

    public double EstimatedSeconds {
      get {
        return this.EstimatedTime.Seconds;
      }
      set {
        this.EstimatedTime = new TimeSpan((long)(value * 1000));
      }
    }
    public double EstimatedMinutes {
      get {
        return this.EstimatedTime.Minutes;
      }
      set {
        this.EstimatedTime = new TimeSpan((long)(value * 60000));
      }
    }
    public ulong EstimatedTicks {
      get {
        return (ulong)this.EstimatedTime.Ticks;
      }
      set {
        this.EstimatedTime = new TimeSpan((long)value);
      }
    }

    public TimeSpan? _estimated = null;
    public TimeSpan EstimatedTime {
      get {
        if (!_estimated.HasValue)
          this.EstimatedTicks = (ulong)(1000 * this.FileKB * HugeFileTimeOutMultiplier / TypicalBytesPerSecond);
        //return new System.TimeSpan(0);
        return _estimated.Value;
      }
      set {
        _estimated = value;
      }
    }

    /// <summary>
    /// Number of ticks before the operation should time out
    /// based on estimates and file size
    /// </summary>
    public ulong TimeOutTicks {
      get {
        return (ulong)(this.EstimatedTicks * HugeFileTimeOutMultiplier);
        // HACK x2 added because stuff was timing out too often
      }
    }

    public TimeSpan EstimateUploadTime(System.IO.FileInfo fi) {
      this.FileBytes = (ulong)fi.Length;
      return this.EstimatedTime;
    }
    public TimeSpan EstimateUploadTime(File file) {
      //file.EnsureProperty(file, f => f.Length);
      this.FileBytes = (ulong)file.Length;
      return this.EstimatedTime;
    }

    public override string ToString() {
      return string.Format("File size is {0:0} KBytes; Estimated upload time {1:0} seconds", this.FileKB, this.EstimatedSeconds);
    }

    /// <summary>
    /// Used to record actual performance of uploads
    /// Advanced versions may provide estimated data
    /// based on actual past results
    /// </summary>
    public TimeSpan ActualTime { get; set; }

  }

}
