using System.Diagnostics;
using System_Monitor_API_v2.Models;   // ← Your SensorInfo lives here

namespace System_Monitor_API_v2.Services;

public class LinuxHardwareMonitor(ILogger<LinuxHardwareMonitor> logger) : INativeHardwareMonitor
{
    private const string HwMonPath = "/sys/class/hwmon";
    private const string PowerCapPath = "/sys/class/powercap";
    private const string ThermalPath = "/sys/class/thermal";
    private const string DrmPath = "/sys/class/drm";

    // ====================== INTERFACE IMPLEMENTATION ======================

    public IReadOnlyList<SensorInfo> GetCpuPowers()
    {
        var powers = new List<SensorInfo>();
        powers.AddRange(FindHwMonPowers(nameFilter: "k10temp"));
        powers.AddRange(GetAllIntelRaplPowers());
        return powers;
    }

    public IReadOnlyList<SensorInfo> GetGpuPowers()
    {
        var powers = new List<SensorInfo>();
        powers.AddRange(GetAllNvidiaGpuPowers());
        powers.AddRange(FindHwMonPowers(nameFilter: "amdgpu"));
        powers.AddRange(FindHwMonPowers(nameFilter: "i915"));
        powers.AddRange(FindHwMonPowers(nameFilter: "xe"));
        return powers;
    }

    public IReadOnlyList<SensorInfo> GetCpuTemperatures()
    {
        var temps = new List<SensorInfo>();
        temps.AddRange(FindHwmonTemps(["Package id", "Tdie", "Tctl", "CPU", "coretemp", "k10temp"]));
        temps.AddRange(ReadAllThermalZoneTemps());
        return temps.DistinctBy(s => s.Name).ToList();
    }

    public IReadOnlyList<SensorInfo> GetGpuTemperatures()
    {
        var temps = new List<SensorInfo>();
        temps.AddRange(GetAllNvidiaGpuTemps());
        temps.AddRange(FindHwmonTemps(["edge", "junction", "GPU", "amdgpu", "nvidia", "radeon", "i915", "xe", "GT", "Graphics"
        ]));
        return temps.DistinctBy(s => s.Name).ToList();
    }

    public IReadOnlyList<SensorInfo> GetGpuLoads()
    {
        var loads = new List<SensorInfo>();
        loads.AddRange(GetAllNvidiaGpuLoads());
        loads.AddRange(ReadDrmGpuBusyLoads());
        loads.AddRange(FindHwMonGpuBusyPercent());
        return loads.DistinctBy(s => s.Name).ToList();
    }

    public IReadOnlyList<SensorInfo> GetGpuClocks()
    {
        var clocks = new List<SensorInfo>();
        clocks.AddRange(GetAllNvidiaGpuClocks());
        clocks.AddRange(ReadDrmGpuClocks());
        clocks.AddRange(FindHwMonGpuClocks());
        return clocks.DistinctBy(s => s.Name).ToList();
    }

    // ====================== PRIVATE HELPERS ======================

    private static List<SensorInfo> FindHwmonTemps(string[] keywords)
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(HwMonPath)) return result;

        foreach (var hwMonDir in Directory.GetDirectories(HwMonPath, "hwmon*"))
        {
            var adapterName = ReadFileSafe(Path.Combine(hwMonDir, "name"))?.Trim() ?? "unknown";
            var isIntelGpu = adapterName.Contains("i915", StringComparison.OrdinalIgnoreCase) ||
                              adapterName.Contains("xe", StringComparison.OrdinalIgnoreCase);

            for (var i = 1; i <= 12; i++)
            {
                var labelPath = Path.Combine(hwMonDir, $"temp{i}_label");
                var inputPath = Path.Combine(hwMonDir, $"temp{i}_input");
                if (!File.Exists(inputPath)) continue;

                var label = ReadFileSafe(labelPath)?.Trim() ?? "";
                var sensorName = string.IsNullOrEmpty(label)
                    ? $"{adapterName} temp{i}"
                    : $"{adapterName} {label}";

                var shouldInclude = keywords.Any(k =>
                    label.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                    adapterName.Contains(k, StringComparison.OrdinalIgnoreCase));

                if (!shouldInclude && (!isIntelGpu || i != 1)) continue;
                if (!long.TryParse(ReadFileSafe(inputPath), out var milliC)) continue;
                var tempC = milliC / 1000.0;
                var displayName = isIntelGpu ? $"Intel GPU ({adapterName})" : sensorName;
                result.Add(new SensorInfo(displayName, tempC));
            }
        }
        return result;
    }

    private static List<SensorInfo> FindHwMonPowers(string? nameFilter = null)
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(HwMonPath)) return result;

        foreach (var hwMonDir in Directory.GetDirectories(HwMonPath, "hwmon*"))
        {
            var adapterName = ReadFileSafe(Path.Combine(hwMonDir, "name")) ?? "";
            if (!string.IsNullOrEmpty(nameFilter) &&
                !adapterName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            for (var i = 1; i <= 5; i++)
            {
                var powerPath = Path.Combine(hwMonDir, $"power{i}_input");
                if (!File.Exists(powerPath)) continue;

                var displayName = (adapterName.Contains("i915", StringComparison.OrdinalIgnoreCase) ||
                                   adapterName.Contains("xe", StringComparison.OrdinalIgnoreCase))
                    ? $"Intel GPU ({adapterName})"
                    : adapterName;

                if (long.TryParse(ReadFileSafe(powerPath), out var microW))
                    result.Add(new SensorInfo(displayName, microW / 1_000_000.0));
            }
        }
        return result;
    }

    private List<SensorInfo> GetAllNvidiaGpuTemps()
        => GetAllNvidiaGpuData("temperature.gpu", "NVIDIA GPU {0} - {1}");

    private List<SensorInfo> GetAllNvidiaGpuPowers()
        => GetAllNvidiaGpuData("power.draw", "NVIDIA GPU {0} - {1}");

    private List<SensorInfo> GetAllNvidiaGpuLoads()
        => GetAllNvidiaGpuData("utilization.gpu", "NVIDIA GPU {0} - {1}");

    private List<SensorInfo> GetAllNvidiaGpuClocks()
        => GetAllNvidiaGpuData("clocks.current.graphics", "NVIDIA GPU {0} - {1}");

    private List<SensorInfo> GetAllNvidiaGpuData(string field, string nameFormat)
    {
        var result = new List<SensorInfo>();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = $"--query-gpu=index,name,{field} --format=csv,noheader,nounits",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null) return result;

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length < 3) continue;

                var valueStr = parts[2].Replace("%", "").Replace("W", "").Replace("MHz", "").Trim();
                if (!double.TryParse(valueStr, out var value)) continue;
                var name = string.Format(nameFormat, parts[0], parts[1]);
                result.Add(new SensorInfo(name, value));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "nvidia-smi query failed for field {Field}", field);
        }
        return result;
    }
    
    private static List<SensorInfo> ReadDrmGpuClocks()
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(DrmPath)) return result;

        foreach (var cardDir in Directory.GetDirectories(DrmPath, "card*"))
        {
            var cardName = Path.GetFileName(cardDir);
            var deviceDir = Path.Combine(cardDir, "device");
            if (!Directory.Exists(deviceDir)) continue;

            // AMD pp_dpm_sclk
            var sClkPath = Path.Combine(deviceDir, "pp_dpm_sclk");
            if (File.Exists(sClkPath))
            {
                var content = ReadFileSafe(sClkPath) ?? "";
                foreach (var line in content.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!line.Trim().EndsWith('*')) continue;
                    var colonParts = line.Split(':', StringSplitOptions.TrimEntries);
                    if (colonParts.Length <= 1) continue;
                    var freqPart = colonParts[1].Split(['M', 'm', 'H', 'h'], StringSplitOptions.TrimEntries)[0];
                    if (int.TryParse(freqPart, out var mhz))
                        result.Add(new SensorInfo($"AMD GPU ({cardName}) Core Clock", mhz));
                }
            }

            // Intel Xe / i915 cur_freq_mhz
            var freqFiles = Directory.GetFiles(deviceDir, "*cur_freq_mhz*", SearchOption.AllDirectories);
            foreach (var f in freqFiles)
            {
                if (long.TryParse(ReadFileSafe(f), out long mhz))
                    result.Add(new SensorInfo($"Intel GPU ({cardName}) Core Clock", mhz));
            }
        }
        return result;
    }

    private static List<SensorInfo> FindHwMonGpuClocks()
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(HwMonPath)) return result;

        foreach (var hwMonDir in Directory.GetDirectories(HwMonPath, "hwmon*"))
        {
            var adapterName = ReadFileSafe(Path.Combine(hwMonDir, "name")) ?? "unknown";

            for (var i = 1; i <= 5; i++)
            {
                var freqPath = Path.Combine(hwMonDir, $"freq{i}_input");
                if (!File.Exists(freqPath) || !long.TryParse(ReadFileSafe(freqPath), out var hz)) continue;
                var mhz = hz / 1_000_000.0;
                var name = adapterName.Contains("amdgpu", StringComparison.OrdinalIgnoreCase)
                    ? $"AMD GPU ({adapterName}) Core Clock"
                    : $"GPU ({adapterName}) Core Clock";
                result.Add(new SensorInfo(name, Math.Round(mhz, 1)));
            }
        }
        return result;
    }

    private static List<SensorInfo> ReadDrmGpuBusyLoads()
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(DrmPath)) return result;

        foreach (var cardDir in Directory.GetDirectories(DrmPath, "card*"))
        {
            var cardName = Path.GetFileName(cardDir);
            var deviceDir = Path.Combine(cardDir, "device");
            if (!Directory.Exists(deviceDir)) continue;

            var gpuBusyPath = Path.Combine(deviceDir, "gpu_busy_percent");
            if (File.Exists(gpuBusyPath) && int.TryParse(ReadFileSafe(gpuBusyPath), out var percent))
                result.Add(new SensorInfo($"AMD GPU ({cardName})", percent));

            string[] intelPatterns = ["gpu_busy_percent", "gt_busy_percent", "render_busy_percent"];
            foreach (var pattern in intelPatterns)
            {
                var files = Directory.GetFiles(deviceDir, pattern);
                foreach (var f in files)
                {
                    if (!int.TryParse(ReadFileSafe(f), out var val)) continue;
                    var load = val > 100 ? val / 10.0 : val;
                    result.Add(new SensorInfo($"Intel GPU ({cardName})", Math.Round(load, 1)));
                }
            }
        }
        return result;
    }

    private static List<SensorInfo> FindHwMonGpuBusyPercent()
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(HwMonPath)) return result;

        foreach (var hwMonDir in Directory.GetDirectories(HwMonPath, "hwmon*"))
        {
            var adapterName = ReadFileSafe(Path.Combine(hwMonDir, "name")) ?? "unknown";
            var busyPath = Path.Combine(hwMonDir, "gpu_busy_percent");
            if (!File.Exists(busyPath) || !int.TryParse(ReadFileSafe(busyPath), out var percent)) continue;
            var name = adapterName.Contains("amdgpu", StringComparison.OrdinalIgnoreCase)
                ? $"AMD GPU ({adapterName})"
                : $"GPU ({adapterName})";
            result.Add(new SensorInfo(name, percent));
        }
        return result;
    }

    private List<SensorInfo> GetAllIntelRaplPowers()
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(PowerCapPath)) return result;

        foreach (var dir in Directory.GetDirectories(PowerCapPath, "intel-rapl:*"))
        {
            var nameFile = Path.Combine(dir, "name");
            var energyFile = Path.Combine(dir, "energy_uj");
            if (!File.Exists(energyFile)) continue;

            var packageName = ReadFileSafe(nameFile)?.Trim() ?? "package";

            try
            {
                var energy1 = long.Parse(ReadFileSafe(energyFile) ?? "0");
                Thread.Sleep(250);
                var energy2 = long.Parse(ReadFileSafe(energyFile) ?? "0");

                var deltaJ = (energy2 - energy1) / 1_000_000.0;
                var powerW = Math.Round(deltaJ / 0.25, 1);
                result.Add(new SensorInfo($"CPU {packageName}", powerW));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reading intel-rapl powers file");
            }
        }
        return result;
    }

    private static List<SensorInfo> ReadAllThermalZoneTemps()
    {
        var result = new List<SensorInfo>();
        if (!Directory.Exists(ThermalPath)) return result;

        foreach (var zoneDir in Directory.GetDirectories(ThermalPath, "thermal_zone*"))
        {
            var type = ReadFileSafe(Path.Combine(zoneDir, "type"))?.Trim() ?? "unknown";
            var tempPath = Path.Combine(zoneDir, "temp");

            if (!File.Exists(tempPath) || !long.TryParse(ReadFileSafe(tempPath), out var milliC)) continue;
            if (type.Contains("cpu", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("package", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("core", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(new SensorInfo($"thermal_zone {type}", milliC / 1000.0));
            }
        }
        return result;
    }

    private static string? ReadFileSafe(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
