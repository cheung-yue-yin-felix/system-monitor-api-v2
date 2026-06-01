using System.Runtime.CompilerServices;
using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public class HardwareMetricPoller(ICrossPlatformHardwareMonitor hardware, ILogger<HardwareMetricPoller> logger): IHardwareMetricPoller
{
    public async IAsyncEnumerable<HardwareMetrics> StreamAsync(
        TimeSpan interval, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            yield return hardware.GetHardwareInfo();

            try
            {
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex, "OperationCanceledException");
                yield break;
            }
        }
    }
}