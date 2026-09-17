using System.Diagnostics;
using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

public class ZModeler3NotFoundException : Exception
{
    public ZModeler3NotFoundException(string message) : base(message)
    {
    }
}

/// <summary>
/// Finds the running ZModeler3 window by process, so the rest of the automation code can walk
/// its UI Automation tree. This only ever reads/interacts through the same public Windows
/// accessibility APIs a screen reader would use - never process memory, never anything
/// ZModeler3 itself doesn't already expose.
/// </summary>
public static class ZModeler3Connection
{
    public static AutomationElement FindMainWindow()
    {
        var process = Process.GetProcesses()
            .FirstOrDefault(p => TryGetTitle(p)?.Contains("ZModeler", StringComparison.OrdinalIgnoreCase) == true);

        if (process is null)
        {
            throw new ZModeler3NotFoundException(
                "Couldn't find a running ZModeler3 window. Make sure ZModeler3 is open with a model loaded.");
        }

        if (process.MainWindowHandle == IntPtr.Zero)
        {
            throw new ZModeler3NotFoundException(
                "Found the ZModeler3 process, but it doesn't have a visible main window yet.");
        }

        return AutomationElement.FromHandle(process.MainWindowHandle);
    }

    private static string? TryGetTitle(Process process)
    {
        try
        {
            return process.MainWindowTitle;
        }
        catch
        {
            return null;
        }
    }
}
