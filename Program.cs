
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using System_Monitor_API_v2.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<ICrossPlatformHardwareMonitor, CrossPlatformHardwareMonitor>();
builder.Services.AddSingleton<IHardwareMetricPoller, HardwareMetricPoller>();

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

app.MapGet("api/metrics", (ICrossPlatformHardwareMonitor crossPlatformHardwareMonitor) =>
    {
        var metrics = crossPlatformHardwareMonitor.GetHardwareInfo();
        return Results.Ok(metrics);
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
        var json = JsonSerializer.Serialize(metrics);
        await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);
    }
}).WithName("GetMetricsStream");

app.Run(); 