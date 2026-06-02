using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public class HardwareMetricsCache(
    IHardwareMetricPoller poller,
    ILogger<HardwareMetricsCache> logger) : IHostedService, IHardwareMetricsCache
{
    private HardwareMetrics? _latest;
    private CancellationTokenSource? _cts;
    private Task? _executingTask;

    public HardwareMetrics? Latest => _latest;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var metrics in poller.StreamAsync(TimeSpan.FromSeconds(1), _cts.Token))
                {
                    _latest = metrics;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in metrics caching loop");
            }
        }, _cts.Token);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            if (_executingTask != null)
            {
                await Task.WhenAny(_executingTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            }
            _cts.Dispose();
        }
    }
}
