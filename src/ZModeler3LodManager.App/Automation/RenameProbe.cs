using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Diagnostic-only probe: selects the first row in the Scene nodes browser and sends F2, so we
/// can inspect what appears afterwards (does an editable control show up? what does UI
/// Automation say about it?) instead of guessing at ZModeler3's rename interaction blind.
/// Doesn't type anything or commit any change - F2 alone doesn't rename, it only (if anything)
/// enters edit mode with the existing name intact.
/// </summary>
public static class RenameProbe
{
    public static string TryF2OnFirstRow(AutomationElement mainWindow)
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

        if (firstRow.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObj))
        {
            ((SelectionItemPattern)patternObj).Select();
        }

        firstRow.SetFocus();
        Thread.Sleep(200);
        SendKeys.SendWait("{F2}");
        Thread.Sleep(200);

        return $"Selected row \"{name}\" and sent F2. Click \"Inspect window\" now to see what changed.";
    }
}
