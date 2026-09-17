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
/// Finds the running ZModeler3 window by process name (not window title - a window title
/// search for "ZModeler" also matches things like an Explorer/archiver window previewing
/// "ZModeler3LodManager.zip", which is exactly this project's own name), so the rest of the
/// automation code can walk its UI Automation tree. This only ever reads/interacts through the
/// same public Windows accessibility APIs a screen reader would use - never process memory,
/// never anything ZModeler3 itself doesn't already expose.
/// </summary>
public static class ZModeler3Connection
{
    private const string ProcessName = "ZModeler3";

    public static AutomationElement FindMainWindow()
    {
        var process = Process.GetProcessesByName(ProcessName).FirstOrDefault(HasVisibleMainWindow);

        if (process is null)
        {
            throw new ZModeler3NotFoundException(
                $"Couldn't find a running \"{ProcessName}.exe\" process with a visible window. " +
                "Make sure ZModeler3 itself is open with a model loaded.");
        }

        return AutomationElement.FromHandle(process.MainWindowHandle);
    }

    private static bool HasVisibleMainWindow(Process process)
    {
        try
        {
            return process.MainWindowHandle != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }
}
