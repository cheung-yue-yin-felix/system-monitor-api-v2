namespace System_Monitor_API_v2.Models;

public class DiskMetrics
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public List<PartitionMetrics> Partitions { get; set; } = [];
}

public class PartitionMetrics
{
    public string Name { get; set; } = "";
    public List<VolumeMetrics> Volumes = new List<VolumeMetrics>();
}

public class VolumeMetrics
{
    public string Name { get; set; } = "";
    public string FreeSpace { get; set; } = "";
    public string TotalSpace { get; set; } = "";
}