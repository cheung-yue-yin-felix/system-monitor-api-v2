using System.Text.Json.Serialization;

namespace System_Monitor_API_v2.Models;

public class HardwareMetrics
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<CpuMetrics>? Cpus { get; set; } = null;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CpuMetrics? Cpu { get; set; } = null;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<GpuMetrics>? Gpus { get; set; } = null;
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GpuMetrics? Gpu { get; set; } = null;
    
    public RamMetrics Ram { get; set; } = new RamMetrics();
    
    public List<DiskMetrics> Disks { get; set; } = [];
}