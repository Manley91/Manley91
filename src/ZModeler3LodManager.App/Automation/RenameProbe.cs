using System.Threading;
using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Diagnostic-only probe: Alt+left-clicks the first row in the Scene nodes browser (confirmed
/// to be ZModeler3's own rename trigger) so we can inspect what appears afterwards, instead of
/// guessing at ZModeler3's rename interaction blind. Doesn't type anything or commit any change.
/// </summary>
public static class RenameProbe
{
    public static string TryAltClickOnFirstRow(AutomationElement mainWindow)
    {
        var grid = ZModeler3ScenePanel.FindSceneNodesGrid(mainWindow);
        if (grid is null)
        {
            return "Couldn't find the Scene nodes browser grid.";
        }

        var rows = ZModeler3ScenePanel.GetRows(grid);
        if (rows.Count == 0)
        {
            return "Scene nodes browser grid has no rows (is a model loaded?).";
        }

        var firstRow = rows[0];
        var name = firstRow.Current.Name;

        var rect = firstRow.Current.BoundingRectangle;
        var x = (int)(rect.Left + (rect.Width / 2));
        var y = (int)(rect.Top + (rect.Height / 2));

        NativeInput.AltLeftClick(x, y);
        Thread.Sleep(200);

        return $"Alt+clicked row \"{name}\" at ({x},{y}). Click \"Inspect window\" now to see what changed.";
    }
}
