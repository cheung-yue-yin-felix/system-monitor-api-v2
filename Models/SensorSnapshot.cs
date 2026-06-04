namespace System_Monitor_API_v2.Models;

public class SensorSnapshot
{
    public List<SensorInfo> CpuTemperatures { get; set; } = [];
    public List<SensorInfo> CpuPowers { get; set; } = [];
    public List<SensorInfo> CpuLoads { get; set; } = [];
    public List<SensorInfo> GpuTemperatures { get; set; } = [];
    public List<SensorInfo> GpuPowers { get; set; } = [];
    public List<SensorInfo> GpuLoads { get; set; } = [];
    public List<SensorInfo> GpuClocks { get; set; } = [];
}
