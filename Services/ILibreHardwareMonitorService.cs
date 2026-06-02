using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public interface ILibreHardwareMonitorService
{
    IReadOnlyList<SensorInfo> GetCpuPowers();
    IReadOnlyList<SensorInfo> GetGpuPowers();
    IReadOnlyList<SensorInfo> GetCpuTemperatures();
    IReadOnlyList<SensorInfo> GetGpuTemperatures();
    IReadOnlyList<SensorInfo> GetGpuLoads();
    IReadOnlyList<SensorInfo> GetGpuClocks();
}