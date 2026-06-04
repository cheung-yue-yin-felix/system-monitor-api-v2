using Hardware.Info;
using System_Monitor_API_v2.Models;
using System_Monitor_API_v2.Utils;

namespace System_Monitor_API_v2.Services;

public class HardwareInfoService(ILogger<HardwareInfoService> logger, ILibreHardwareMonitorService libreHardwareMonitorService) : IHardwareInfoService
{
    private readonly HardwareInfo _hardwareInfo = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public HardwareMetrics GetHardwareInfo()
    {
        _semaphore.Wait();
        try
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

            var snapshot = libreHardwareMonitorService.GetSnapshot();

            FillCpuInfo(hardwareMetrics, snapshot);
            
            FillGpuInfo(hardwareMetrics, snapshot);

            FillMemoryInfo(hardwareMetrics);

            FillDiskInfo(hardwareMetrics);
            
            FillNetworkInfo(hardwareMetrics);

            return hardwareMetrics;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void FillCpuInfo(HardwareMetrics hardwareMetrics, SensorSnapshot snapshot)
    {
        var cpuList = _hardwareInfo.CpuList.Select(cpu => 
            new CpuMetrics
            {
                Name = cpu.Name.TrimEnd(),
                ClockSpeed = $"{cpu.CurrentClockSpeed:N} MHz"
            }).ToList();

        if (snapshot.CpuLoads.Count > 0)
            cpuList.ForEach(cpu => cpu.Load = $"{snapshot.CpuLoads.First(x => cpu.Name.Contains(x.Name)).Value}%");
        
        if (snapshot.CpuTemperatures.Count > 0)
            cpuList.ForEach(cpu => cpu.Temperature = $"{snapshot.CpuTemperatures.First(x => cpu.Name.Contains(x.Name)).Value:F2}°C");

        if (snapshot.CpuPowers.Count > 0)
            cpuList.ForEach(cpu => cpu.Power = $"{snapshot.CpuPowers.First(x => cpu.Name.Contains(x.Name)).Value:F2} W");
        
        if (cpuList.Count > 1)
            hardwareMetrics.Cpus = cpuList;
        else
            hardwareMetrics.Cpu = cpuList[0];
    }

    private void FillGpuInfo(HardwareMetrics hardwareMetrics, SensorSnapshot snapshot)
    {
        var gpuList = _hardwareInfo.VideoControllerList.Select(gpu => 
            new GpuMetrics()
            {
                Name = gpu.Name.TrimEnd(),
                VideoRamSize = ByteFormatter.BytesToGiB((long)gpu.AdapterRAM)
            }).ToList();

        if (snapshot.GpuClocks.Count > 0)
            gpuList.ForEach(gpu => gpu.ClockSpeed = $"{snapshot.GpuClocks.First(x => gpu.Name.Contains(x.Name)).Value:N} MHz");
        
        if (snapshot.GpuTemperatures.Count > 0)
            gpuList.ForEach(gpu => gpu.Temperature = $"{snapshot.GpuTemperatures.First(x => gpu.Name.Contains(x.Name)).Value:F2}°C");
        
        if (snapshot.GpuLoads.Count > 0)
            gpuList.ForEach(gpu => gpu.Load = $"{snapshot.GpuLoads.First(x => gpu.Name.Contains(x.Name)).Value:F2}%");
        
        if (snapshot.GpuPowers.Count > 0)
            gpuList.ForEach(gpu => gpu.Power = $"{snapshot.GpuPowers.First(x => gpu.Name.Contains(x.Name)).Value:F2} W");
        
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
                Partitions = disk.GetPartitions()
            });
        
        hardwareMetrics.Disks = diskMetrics.ToList();
    }

    private void FillNetworkInfo(HardwareMetrics hardwareMetrics)
    {
        _hardwareInfo.NetworkAdapterList.ForEach(nic =>
        {
            hardwareMetrics.Networks.Add(
                new NetworkMetrics()
                {
                    Name = nic.Name,
                    Type = nic.AdapterType,
                    DhcpServer = nic.DHCPServer.ToString(),
                    MacAddress = nic.MACAddress,
                    IpAddresses = nic.IPAddressList.Select(ip => ip.ToString()).ToList(),
                    IpSubnets = nic.IPSubnetList.Select(ip => ip.ToString()).ToList(),
                    DefaultIpGateways = nic.DefaultIPGatewayList.Select(ip => ip.ToString()).ToList(),
                    DownloadSpeed = $"{ByteFormatter.BytesToMiB((long)nic.BytesReceivedPersec)}/s",
                    UploadSpeed = $"{ByteFormatter.BytesToMiB((long)nic.BytesSentPersec)}/s"
                });
        });
    }
}
