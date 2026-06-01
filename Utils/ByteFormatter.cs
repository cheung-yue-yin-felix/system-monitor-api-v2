namespace System_Monitor_API_v2.Utils;

public static class ByteFormatter
{
    private const long BytesPerKiB = 1_024L;
    private const long BytesPerMiB = 1_048_576L;
    private const long BytesPerGiB = 1_073_741_824L;
    private const long BytesPerTiB = 1_099_511_627_776L;   // 1024^4
    
    extension(long bytes)
    {
        private string ToTiB() 
            => $"{bytes / (double)BytesPerTiB:F2} TiB";

        private string ToTb() 
            => $"{bytes / (double)BytesPerTiB:F2} TB";

        private string ToGiB() 
            => $"{bytes / (double)BytesPerGiB:F2} GiB";

        private string ToMiB() 
            => $"{bytes / (double)BytesPerMiB:F0} MiB";

        private string Format()
        {
            return bytes switch
            {
                < BytesPerKiB => $"{bytes} B",
                < BytesPerMiB => $"{bytes / (double)BytesPerKiB:F1} KiB",
                < BytesPerGiB => $"{bytes / (double)BytesPerMiB:F1} MiB",
                < BytesPerTiB => $"{bytes / (double)BytesPerGiB:F2} GiB",
                _ => $"{bytes / (double)BytesPerTiB:F2} TiB"
            };
        }
    }

    public static string BytesToTiB(long bytes) => bytes.ToTiB();
    public static string BytesToTb(long bytes) => bytes.ToTb();
    public static string BytesToGiB(long bytes) => bytes.ToGiB();
    public static string BytesToMiB(long bytes) => bytes.ToMiB();
    public static string FormatBytes(long bytes) => bytes.Format();
}