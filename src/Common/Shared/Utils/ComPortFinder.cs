using System.IO.Ports;

namespace GEC.Common.Shared.Utils;

public static class ComPortFinder
{
    public static string? FindFirstComPortName()
    {
        if (OperatingSystem.IsWindows())
        {
            return FindFirstWindowsPort();
        }

        if (OperatingSystem.IsLinux())
        {
            return FindFirstLinuxUsbSerialPort();
        }

        return null;
    }

    private static string? FindFirstWindowsPort()
    {
        var ports = SerialPort.GetPortNames();

        return ports
            .OrderBy(p => ExtractComNumber(p))
            .ThenBy(p => p, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static int ExtractComNumber(string portName)
    {
        if (portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(portName.AsSpan(3), out var n))
        {
            return n;
        }

        return int.MaxValue;
    }

    private static string? FindFirstLinuxUsbSerialPort()
    {
        const string byIdDir = "/dev/serial/by-id";
        if (Directory.Exists(byIdDir))
        {
            var candidate = Directory.GetFiles(byIdDir)
                .Select(path => new
                {
                    LinkPath = path,
                    TargetName = Path.GetFileName(new FileInfo(path).ResolveLinkTarget(true)?.FullName ?? string.Empty)
                })
                .Where(x => x.TargetName.StartsWith("ttyUSB", StringComparison.Ordinal) ||
                            x.TargetName.StartsWith("ttyACM", StringComparison.Ordinal))
                .OrderBy(x => x.LinkPath, StringComparer.Ordinal)
                .Select(x => x.LinkPath)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        var usb = Directory.GetFiles("/dev", "ttyUSB*")
                           .OrderBy(p => p, StringComparer.Ordinal)
                           .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(usb))
        {
            return usb;
        }

        var acm = Directory.GetFiles("/dev", "ttyACM*")
                           .OrderBy(p => p, StringComparer.Ordinal)
                           .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(acm))
        {
            return acm;
        }

        return null;
    }
}
