using Hardware.Info;
using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Utils;

public static class DiskHelper
{
    public static List<PartitionMetrics> GetPartitions(this Drive disk)
    {
        return disk.PartitionList
            .Where(partition => partition is { PrimaryPartition: true, VolumeList.Count: > 0 })
            .Select(partition =>
                new PartitionMetrics()
                {
                    Name = partition.Name,
                    Volumes = partition.GetVolumes()
                }).ToList();
    }
    
    private static List<VolumeMetrics> GetVolumes(this Partition partition)
    {
        return partition.VolumeList.Select(volume =>
            new VolumeMetrics()
            {
                Name = volume.Name,
                FreeSpace = ByteFormatter.FormatBytes((long)volume.FreeSpace),
                TotalSpace = ByteFormatter.FormatBytes((long)volume.Size)
            }).ToList();
    }
}