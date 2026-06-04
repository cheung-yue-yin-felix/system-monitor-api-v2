using LibreHardwareMonitor.Hardware;
using System_Monitor_API_v2.Models;
using System_Monitor_API_v2.Utils;

namespace System_Monitor_API_v2.Services;

public class LibreHardwareMonitorService(ILogger<LibreHardwareMonitorService> logger) : ILibreHardwareMonitorService, IDisposable
{
    private readonly Computer _computer = InitiateComputer();
    private bool _isOpen;
    private bool _disposed;

    private static Computer InitiateComputer()
    {
        return new Computer()
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsPowerMonitorEnabled = true
        };
    }

    private void EnsureOpen()
    {
        if (_isOpen) return;
        _computer.Open();
        _isOpen = true;
    }

    public SensorSnapshot GetSnapshot()
    {
        var snapshot = new SensorSnapshot();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType == HardwareType.Cpu)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Value is null) continue;
                    var value = double.Parse(sensor.Value.ToString() ?? "0.00");

                    if (sensor.SensorType == SensorType.Power && sensor.Name == "Package")
                        snapshot.CpuPowers.Add(new SensorInfo(hardware.Name, value));

                    if (sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total")
                        snapshot.CpuLoads.Add(new SensorInfo(hardware.Name, value));

                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        if (hardware.Name.Contains("AMD") && sensor.Name == "Core (Tctl/Tdie)")
                            snapshot.CpuTemperatures.Add(new SensorInfo(hardware.Name, value));
                        else if (!hardware.Name.Contains("AMD") && sensor.Name == "Core Package")
                            snapshot.CpuTemperatures.Add(new SensorInfo(hardware.Name, value));
                    }
                }
            }
            else if (hardware.HardwareType is HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia)
            {
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Value is null) continue;
                    var value = double.Parse(sensor.Value.ToString() ?? "0.00");

                    if (sensor.SensorType == SensorType.Power && sensor.Name == "GPU Package")
                        snapshot.GpuPowers.Add(new SensorInfo(hardware.Name, value));

                    if (sensor.SensorType == SensorType.Temperature && sensor.Name == "GPU Core")
                        snapshot.GpuTemperatures.Add(new SensorInfo(hardware.Name, value));

                    if (sensor.SensorType == SensorType.Load && sensor.Name == "GPU Core")
                        snapshot.GpuLoads.Add(new SensorInfo(hardware.Name, value));

                    if (sensor.SensorType == SensorType.Clock && sensor.Name == "GPU Core")
                        snapshot.GpuClocks.Add(new SensorInfo(hardware.Name, value));
                }
            }
        }

        return snapshot;
    }

    public IReadOnlyList<SensorInfo> GetCpuPowers()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu) continue;
            
            hardware.Update();
            
            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Power && sensor.Name == "Package"
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetGpuPowers()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;
            
            hardware.Update();
            
            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Power && sensor.Name == "GPU Package"
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetCpuTemperatures()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu) continue;

            hardware.Update();
            
            if (hardware.Name.Contains("AMD"))
                result.AddRange(
                    from sensor in hardware.Sensors
                    where sensor.SensorType == SensorType.Temperature && sensor.Name == "Core (Tctl/Tdie)"
                    select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
            else
                result.AddRange(
                    from sensor in hardware.Sensors
                    where sensor.SensorType == SensorType.Temperature && sensor.Name == "Core Package"
                    select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }
    
    public IReadOnlyList<SensorInfo> GetGpuTemperatures()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            hardware.Update();
            
            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Temperature && sensor.Name == "GPU Core"
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetGpuLoads()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            hardware.Update();
            
            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Load && sensor.Name == "GPU Core"
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetGpuClocks()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            hardware.Update();
            
            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Clock && sensor.Name == "GPU Core"
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetCpuLoads()
    {
        var result = new List<SensorInfo>();
        EnsureOpen();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu) continue;
            
            hardware.Update();
            
            result.AddRange(
                from sensor in hardware.Sensors
                where sensor.SensorType == SensorType.Load && sensor.Name == "CPU Total"
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        return result.AsReadOnly();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_isOpen) return;
        _computer.Close();
        _isOpen = false;
        GC.SuppressFinalize(this);
    }
}
