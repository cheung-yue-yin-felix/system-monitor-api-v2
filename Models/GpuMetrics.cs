namespace System_Monitor_API_v2.Models;

public class GpuMetrics
{
    public string Name { get; init; } = "";
    public string ClockSpeed { get; set; } = "";
    public string VideoRamSize { get; set; } = "";
    public string Load { get; set; } = "";
    public string Temperature { get; set; } = "";
    public string Power { get; set; } = "";
}