using System.Runtime.CompilerServices;
using System.Threading.Channels;
using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public class HardwareMetricsCache(
    IHardwareMetricPoller poller,
    ILogger<HardwareMetricsCache> logger) : IHostedService, IHardwareMetricsCache
{
    private CancellationTokenSource? _cts;
    private Task? _executingTask;
    private readonly Channel<HardwareMetrics> _channel = Channel.CreateUnbounded<HardwareMetrics>();

    public HardwareMetrics? Latest { get; private set; }

    public IAsyncEnumerable<HardwareMetrics> StreamAsync(CancellationToken cancellationToken = default)
    {
        return StreamInternalAsync(cancellationToken);
    }

    private async IAsyncEnumerable<HardwareMetrics> StreamInternalAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (Latest is not null)
        {
            yield return Latest;
        }

        await foreach (var metrics in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return metrics;
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var metrics in poller.StreamAsync(TimeSpan.FromSeconds(1), _cts.Token))
                {
                    Latest = metrics;
                    _channel.Writer.TryWrite(metrics);
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
            finally
            {
                _channel.Writer.Complete();
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
