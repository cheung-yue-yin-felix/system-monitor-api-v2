using System.Diagnostics;
using System.Text.Json;
using System_Monitor_API_v2.Authentication;
using System_Monitor_API_v2.Services;

var exePath = Process.GetCurrentProcess().MainModule?.FileName;
var applicationFolder = Path.GetDirectoryName(exePath);

if (!string.IsNullOrEmpty(applicationFolder))
{
    // 2. Force Windows to shift its working directory out of System32 into the real app folder
    Environment.CurrentDirectory = applicationFolder;
}

var isEmbedded = args.Contains("--embedded");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IHardwareInfoService, HardwareInfoService>();
builder.Services.AddSingleton<IHardwareMetricPoller, HardwareMetricPoller>();
builder.Services.AddSingleton<HardwareMetricsCache>();
builder.Services.AddSingleton<IHardwareMetricsCache>(sp => sp.GetRequiredService<HardwareMetricsCache>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<HardwareMetricsCache>());
builder.Services.AddSingleton<ILibreHardwareMonitorService, LibreHardwareMonitorServiceService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        if (isEmbedded)
        {
            // When bundled inside Electron, requests come from file:// protocol
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(
                    "https://cheung-yue-yin-felix.github.io",
                    "https://system-monitor",
                    "http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowFrontend");

if (!isEmbedded)
{
    app.UseHttpsRedirection();
    app.UseMiddleware<ApiKeyMiddleware>();
}

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
