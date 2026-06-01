using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public interface IHardwareMetricPoller
{
    IAsyncEnumerable<HardwareMetrics> StreamAsync(TimeSpan interval, CancellationToken cancellationToken = default);
}