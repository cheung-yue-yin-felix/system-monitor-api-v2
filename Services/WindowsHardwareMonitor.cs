using LibreHardwareMonitor.Hardware;
using System_Monitor_API_v2.Models;
using System_Monitor_API_v2.Utils;

namespace System_Monitor_API_v2.Services;

public class WindowsHardwareMonitor(ILogger<WindowsHardwareMonitor> logger): INativeHardwareMonitor
{
    private readonly Computer _computer = InitiateComputer();

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

    public IReadOnlyList<SensorInfo> GetCpuPowers()
    {
        var result = new List<SensorInfo>();
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            if (hardware.HardwareType != HardwareType.Cpu) continue;
            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Power 
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        _computer.Close();
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetGpuPowers()
    {
        var result = new List<SensorInfo>();
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Power 
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        _computer.Close();
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetCpuTemperatures()
    {
        var result = new List<SensorInfo>();
        _computer.Open();
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            if (hardware.HardwareType != HardwareType.Cpu) continue;

            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Temperature 
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        _computer.Close();
        return result.AsReadOnly();
    }
    
    public IReadOnlyList<SensorInfo> GetGpuTemperatures()
    {
        var result = new List<SensorInfo>();
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Temperature 
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        _computer.Close();
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetGpuLoads()
    {
        var result = new List<SensorInfo>();
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Load 
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        _computer.Close();
        return result.AsReadOnly();
    }

    public IReadOnlyList<SensorInfo> GetGpuClocks()
    {
        var result = new List<SensorInfo>();
        _computer.Open();
        _computer.Accept(new UpdateVisitor());
        foreach (var hardware in _computer.Hardware)
        {
            hardware.Update();
            if (hardware.HardwareType != HardwareType.GpuAmd &&
                hardware.HardwareType != HardwareType.GpuIntel &&
                hardware.HardwareType != HardwareType.GpuNvidia) continue;

            result.AddRange(
                from sensor in hardware.Sensors 
                where sensor.SensorType == SensorType.Clock 
                select new SensorInfo(hardware.Name, double.Parse(sensor.Value.ToString() ?? "0.00")));
        }
        _computer.Close();
        return result.AsReadOnly();
    }
}