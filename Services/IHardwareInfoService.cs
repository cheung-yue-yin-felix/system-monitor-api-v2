using System_Monitor_API_v2.Models;

namespace System_Monitor_API_v2.Services;

public interface IHardwareInfoService
{
    HardwareMetrics GetHardwareInfo();
}