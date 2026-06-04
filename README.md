# System Monitor API v2

A lightweight ASP.NET Core Web API that exposes real-time hardware metrics (CPU, RAM, disk, network, GPU, and sensors) via REST and Server-Sent Events (SSE). It is designed to be consumed by the [System Monitor](https://github.com/cheung-yue-yin-felix/system-monitor) frontend dashboard.

## Features

- **Real-time metrics** – CPU load, RAM usage & module info, disk space, network throughput, GPU stats, and temperature/voltage/fan sensors.
- **REST endpoint** – `GET /api/metrics` returns the latest snapshot as JSON.
- **SSE stream** – `GET /api/metrics/stream` pushes live metrics every second.
- **API-key auth** – simple token-based middleware (disabled when running in embedded/`--embedded` mode).
- **CORS ready** – pre-configured for the System Monitor frontend origins.
- **Embedded mode** – supports bundling inside an Electron app (`--embedded` flag).

## Tech Stack

- **.NET 10**
- **Hardware.Info** & **Hardware.Info.Core** – cross-platform hardware inventory
- **LibreHardwareMonitorLib** – sensor readings (temperatures, voltages, fans, etc.)
- **ASP.NET Core OpenAPI** – OpenAPI / Swagger support in Development

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows (LibreHardwareMonitorLib requires Windows for full sensor support)

### Run locally

```bash
# Clone the repo
git clone https://github.com/cheung-yue-yin-felix/system-monitor-api-v2.git
cd system-monitor-api-v2

# Run in Development (HTTP only)
dotnet run --launch-profile http

# Or with HTTPS
dotnet run --launch-profile https
```

The API will be available at:

| Protocol | URL                   |
|----------|-----------------------|
| HTTP     | http://localhost:5143 |
| HTTPS    | https://localhost:7206|

### API Endpoints

| Method | Endpoint              | Description                              |
|--------|-----------------------|------------------------------------------|
| GET    | `/api/metrics`        | Latest hardware metrics snapshot (JSON)  |
| GET    | `/api/metrics/stream` | Server-Sent Events stream (1 msg/sec)    |

> **Auth:** Send the API key in the `X-API-Key` header (value from `appsettings.json`).

### Example request

```bash
curl -H "X-API-Key: uVJ5bzhbqdeKMSzrubreBvuJETKh5IEZ" \
     http://localhost:5143/api/metrics
```

## Configuration

All settings live in `appsettings.json` (or `appsettings.Development.json`):

```json
{
  "AllowedHosts": "*",
  "ApiKey": "uVJ5bzhbqdeKMSzrubreBvuJETKh5IEZ"
}
```

| Key      | Description                                |
|----------|--------------------------------------------|
| `ApiKey` | Token required by `X-API-Key` header       |

## Project Structure

```
System Monitor API v2/
├── Authentication/
│   └── ApiKeyMiddleware.cs          # API key validation
├── Models/
│   ├── CpuMetrics.cs                # CPU data models
│   ├── DiskMetrics.cs               # Disk data models
│   ├── GpuMetrics.cs                # GPU data models
│   ├── HardwareMetrics.cs           # Aggregated root model
│   ├── NetworkMetrics.cs            # Network data models
│   ├── RamMetrics.cs                # RAM data models
│   ├── RamModuleMetrics.cs          # RAM module details
│   └── SensorInfo.cs                # Sensor (temp/voltage/fan) model
├── Services/
│   ├── HardwareInfoService.cs       # Hardware.Info wrapper
│   ├── HardwareMetricPoller.cs      # Background polling & SSE logic
│   ├── HardwareMetricsCache.cs      # Cached latest metrics
│   ├── LibreHardwareMonitorServiceService.cs  # LibreHardwareMonitor wrapper
│   └── *Interfaces*                 # Service contracts
├── Utils/
│   ├── ByteFormatter.cs             # Human-readable byte formatting
│   ├── DiskHelper.cs                # Disk utility helpers
│   └── UpdateVisitor.cs             # LibreHardwareMonitor visitor
├── Program.cs                       # App bootstrap & DI setup
└── appsettings*.json                # Configuration
```

## Publishing

### Self-contained single-file (recommended for distribution)

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true
```

Output will be in `bin/Release/net10.0/win-x64/publish/`.

### Framework-dependent

```bash
dotnet publish -c Release
```

## License

MIT © Felix Cheung
