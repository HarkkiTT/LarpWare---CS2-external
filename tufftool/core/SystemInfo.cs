using System.Management;
using System.Runtime.InteropServices;

namespace TuffTool.Core;

public static class SystemInfo
{
    [DllImport("dxgi.dll")]
    private static extern int DXGID3D10GetDevice1(IntPtr adapter, int driverType, IntPtr pGuid, out IntPtr ppDevice);

    public static string GetGpuName()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(name) && !name.Contains("Microsoft"))
                    {
                        return name;
                    }
                }
            }
        }
        catch { }

        
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController WHERE Name IS NOT NULL"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString() ?? "";
                    if (!string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }
        }
        catch { }

        return "Unknown GPU";
    }

    public static string GetCpuName()
    {
        try
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["Name"]?.ToString() ?? "Unknown CPU";
                }
            }
        }
        catch { }
        return "Unknown CPU";
    }

    public static string GetOsVersion()
    {
        return Environment.OSVersion.ToString();
    }

    public static void PrintSystemInfo()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("[*] LarpWare System Info");
        Console.ResetColor();
        Console.WriteLine("---------------------------------------------");
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[+] ");
        Console.ResetColor();
        Console.WriteLine($"GPU: {GetGpuName()}");
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[+] ");
        Console.ResetColor();
        Console.WriteLine($"CPU: {GetCpuName()}");
        
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[+] ");
        Console.ResetColor();
        Console.WriteLine($"OS: {GetOsVersion()}");
        
        Console.WriteLine();
    }
}
