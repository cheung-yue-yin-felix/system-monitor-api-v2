
using System.Runtime.InteropServices;
using System.Text.Json;
using System_Monitor_API_v2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICrossPlatformHardwareMonitor, CrossPlatformHardwareMonitor>();
builder.Services.AddSingleton<IHardwareMetricPoller, HardwareMetricPoller>();
builder.Services.AddSingleton<HardwareMetricsCache>();
builder.Services.AddSingleton<IHardwareMetricsCache>(sp => sp.GetRequiredService<HardwareMetricsCache>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<HardwareMetricsCache>());

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.Services.AddSingleton<INativeHardwareMonitor, WindowsHardwareMonitor>();
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
    builder.Services.AddSingleton<INativeHardwareMonitor, LinuxHardwareMonitor>();
else
    throw new PlatformNotSupportedException();
    
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
};

app.MapGet("api/metrics", (IHardwareMetricsCache cache) =>
    {
        var metrics = cache.Latest;
        return metrics is not null 
            ? Results.Json(metrics, jsonOptions) 
            : Results.Problem("Metrics not yet available", statusCode: 503);
    })
    .WithName("GetHardwareMetrics");

app.MapGet("api/metrics/stream", async (
    HttpContext context,
    IHardwareMetricPoller poller,
    CancellationToken cancellationToken) =>
{
    context.Response.Headers.Append("Content-Type", "text/event-stream");
    context.Response.Headers.Append("Cache-Control", "no-cache");
    context.Response.Headers.Append("Connection", "keep-alive");
    
    await foreach (var metrics in poller.StreamAsync(TimeSpan.FromSeconds(1), cancellationToken))
    {
        var json = JsonSerializer.Serialize(metrics, jsonOptions);
        await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
}).WithName("GetMetricsStream");

app.Run();
