namespace System_Monitor_API_v2.Models;

public class NetworkMetrics
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DhcpServer { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public List<string> IpAddresses { get; set; } = [];
    public List<string> IpSubnets { get; set; } = [];
    public List<string> DefaultIpGateways { get; set; } = [];
    public string UploadSpeed { get; set; } = string.Empty;
    public string DownloadSpeed { get; set; } = string.Empty;
}