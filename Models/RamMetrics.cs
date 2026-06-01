namespace System_Monitor_API_v2.Models;

public class RamMetrics
{
    public string AvailableMemory { get; set; } = "";
    public string TotalMemory { get; set; } = "";
    public string UsedMemory { get; set; } = "";
    public List<RamModuleMetrics> Modules { get; set; } = [];
}
