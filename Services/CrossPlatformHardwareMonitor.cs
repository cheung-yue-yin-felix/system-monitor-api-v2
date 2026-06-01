using Hardware.Info;
using System_Monitor_API_v2.Models;
using System_Monitor_API_v2.Utils;

namespace System_Monitor_API_v2.Services;

public class CrossPlatformHardwareMonitor(ILogger<CrossPlatformHardwareMonitor> logger, INativeHardwareMonitor nativeHardwareMonitor) : ICrossPlatformHardwareMonitor
{
    private readonly HardwareInfo _hardwareInfo = new();
    
    public HardwareMetrics GetHardwareInfo()
    {
        var hardwareMetrics = new HardwareMetrics();
        
        try
        {
            _hardwareInfo.RefreshAll();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception on refreshing hardware info");
        }

        FillCpuInfo(hardwareMetrics);
        
        FillGpuInfo(hardwareMetrics);

        FillMemoryInfo(hardwareMetrics);

        FillDiskInfo(hardwareMetrics);

        return hardwareMetrics;
    }

    private void FillCpuInfo(HardwareMetrics hardwareMetrics)
    {
        var temperatures = nativeHardwareMonitor.GetCpuTemperatures();
        var powers = nativeHardwareMonitor.GetCpuPowers();
        var cpuList = _hardwareInfo.CpuList.Select(cpu => 
            new CpuMetrics
            {
                Name = cpu.Name.TrimEnd(),
                ClockSpeed = $"{cpu.CurrentClockSpeed:N} MHz",
                Load = $"{(double)cpu.PercentProcessorTime / cpu.NumberOfLogicalProcessors:F2}%"
            }).ToList();

        if (temperatures.Count > 0)
            cpuList.ForEach(cpu => cpu.Temperature = $"{temperatures.First(x => cpu.Name.Contains(x.Name)).Value:F2}°C");

        if (powers.Count > 0)
            cpuList.ForEach(cpu => cpu.Power = $"{powers.First(x => cpu.Name.Contains(x.Name)).Value:F2} W");
        
        if (cpuList.Count > 1)
            hardwareMetrics.Cpus = cpuList;
        else
            hardwareMetrics.Cpu = cpuList[0];
    }

    private void FillGpuInfo(HardwareMetrics hardwareMetrics)
    {
        var loads = nativeHardwareMonitor.GetGpuLoads();
        var temperatures = nativeHardwareMonitor.GetGpuTemperatures();
        var powers = nativeHardwareMonitor.GetGpuPowers();
        var clocks = nativeHardwareMonitor.GetGpuClocks();
        var gpuList = _hardwareInfo.VideoControllerList.Select(gpu => 
            new GpuMetrics()
            {
                Name = gpu.Name.TrimEnd(),
                VideoRamSize = ByteFormatter.BytesToGiB((long)gpu.AdapterRAM)
            }).ToList();

        if (clocks.Count > 0)
            gpuList.ForEach(gpu => gpu.ClockSpeed = $"{clocks.First(x => gpu.Name.Contains(x.Name)).Value:N} MHz");
        
        if (temperatures.Count > 0)
            gpuList.ForEach(gpu => gpu.Temperature = $"{temperatures.First(x => gpu.Name.Contains(x.Name)).Value:F2}°C");
        
        if (loads.Count > 0)
            gpuList.ForEach(gpu => gpu.Load = $"{loads.First(x => gpu.Name.Contains(x.Name)).Value:F2}%");
        
        if (powers.Count > 0)
            gpuList.ForEach(gpu => gpu.Power = $"{powers.First(x => gpu.Name.Contains(x.Name)).Value:F2} W");
        
        if (gpuList.Count > 1)
            hardwareMetrics.Gpus = gpuList;
        else
            hardwareMetrics.Gpu = gpuList[0];
    }

    private void FillMemoryInfo(HardwareMetrics hardwareMetrics)
    {
        var memoryList = _hardwareInfo.MemoryList.Select(memory =>
            new RamModuleMetrics()
            {
                Name = memory.PartNumber ,
                Size = ByteFormatter.BytesToGiB((long)memory.Capacity),
                Type = memory.MemoryType.ToString(),
                FormFactor = memory.FormFactor.ToString()
            });
        
        var memoryMetrics = new RamMetrics()
        {
            AvailableMemory = ByteFormatter.BytesToGiB((long)_hardwareInfo.MemoryStatus.AvailablePhysical),
            TotalMemory = ByteFormatter.BytesToGiB((long)_hardwareInfo.MemoryStatus.TotalPhysical),
            UsedMemory = ByteFormatter.BytesToGiB((long)_hardwareInfo.MemoryStatus.TotalPhysical - (long)_hardwareInfo.MemoryStatus.AvailablePhysical),
            Modules = memoryList.ToList(),
        };
        
        hardwareMetrics.Ram = memoryMetrics;
    }

    private void FillDiskInfo(HardwareMetrics hardwareMetrics)
    {
        var diskMetrics = _hardwareInfo.DriveList.Select(disk =>
            new DiskMetrics()
            {
                Name = disk.Model,
                Type = disk.MediaType,
                Partitions = GetPartitions(disk)
            });
        
        hardwareMetrics.Disks = diskMetrics.ToList();
    }
    
    private List<PartitionMetrics> GetPartitions(Drive disk)
    {
        return disk.PartitionList
            .Where(partition => partition is { PrimaryPartition: true, VolumeList.Count: > 0 })
            .Select(partition =>
            new PartitionMetrics()
            {
                Name = partition.Name,
                Volumes = GetVolumes(partition)
            }).ToList();
    }
    
    private List<VolumeMetrics> GetVolumes(Partition partition)
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