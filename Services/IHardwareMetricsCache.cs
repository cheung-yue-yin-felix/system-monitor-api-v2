using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public interface IHardwareMetricsCache
{
    HardwareMetrics? Latest { get; }
}
