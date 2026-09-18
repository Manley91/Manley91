using System.Threading;
using System.Windows.Automation;
using ZModeler3LodManager.Core.Naming;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Live (no-file) version of "Organize Existing LODs": renames every row currently selected in
/// ZModeler3's Scene nodes browser, in top-to-bottom order, to the configured LOD naming
/// pattern - reusing the same LodNaming logic the file-based workflow uses.
/// </summary>
public static class LiveLodRenamer
{
    public static string RenameSelectedAsLodLevels(AutomationElement mainWindow, LodNamingOptions options, string? baseNameOverride)
    {
        var grid = ZModeler3ScenePanel.FindSceneNodesGrid(mainWindow);
        if (grid is null)
        {
            return "Couldn't find the Scene nodes browser grid.";
        }

        var initialRows = ZModeler3ScenePanel.GetRows(grid);
        var selectedIndices = new List<int>();
        for (var idx = 0; idx < initialRows.Count; idx++)
        {
            if (ZModeler3ScenePanel.IsSelected(initialRows[idx]))
            {
                selectedIndices.Add(idx);
            }
        }

        if (selectedIndices.Count == 0)
        {
            return "Nothing is selected in the Scene nodes browser. Select the variants to " +
                   "rename first (top to bottom = highest to lowest detail).";
        }

        var baseName = string.IsNullOrWhiteSpace(baseNameOverride)
            ? LodNaming.GetBaseName(ZModeler3ScenePanel.SafeName(initialRows[selectedIndices[0]]), options)
            : baseNameOverride!.Trim();

        var log = new List<string>();
        const int maxAttempts = 5;
        var windowHandle = mainWindow.Current.NativeWindowHandle;

        NativeInput.ActivateWindow(windowHandle);
        Thread.Sleep(150);
        NativeInput.SendEscape();
        Thread.Sleep(250);

        // Target rows by their ABSOLUTE position in the grid, fixed up front - not by name
        // (duplicate names all resolve to the same first match) and not by "currently selected"
        // (double-clicking to rename one row drops the rest of the original multi-selection, so
        // re-querying "selected rows" shrinks after every step). Position is stable because
        // renaming a row doesn't reorder the grid.
        for (var i = 0; i < selectedIndices.Count; i++)
        {
            var rowIndex = selectedIndices[i];
            var newName = LodNaming.BuildLodName(baseName, options, i);
            var originalNameForLog = "?";
            var succeeded = false;

            for (var attempt = 1; attempt <= maxAttempts && !succeeded; attempt++)
            {
                var freshRows = GetFreshRows(mainWindow);
                if (rowIndex >= freshRows.Count)
                {
                    log.Add($"[{i}] row index {rowIndex} is out of range (grid now has {freshRows.Count} rows) - stopping.");
                    return BuildResult(log, selectedIndices.Count);
                }

                var target = freshRows[rowIndex];
                originalNameForLog = ZModeler3ScenePanel.SafeName(target);

                if (originalNameForLog == newName)
                {
                    succeeded = true;
                    break;
                }

                var rect = target.Current.BoundingRectangle;
                var x = (int)(rect.Left + (rect.Width / 2));
                var y = (int)(rect.Top + (rect.Height / 2));

                NativeInput.SpamLeftClicksMessage(windowHandle, x, y);
                Thread.Sleep(450);
                NativeInput.SelectAllTypeAndCommit(newName);
                Thread.Sleep(450);
                NativeInput.SendEscape();
                Thread.Sleep(300);

                var verifyRows = GetFreshRows(mainWindow);
                succeeded = rowIndex < verifyRows.Count && ZModeler3ScenePanel.SafeName(verifyRows[rowIndex]) == newName;

                if (!succeeded)
                {
                    log.Add($"[{i}] attempt {attempt} at ({x},{y}) didn't take - retrying" +
                            (attempt == maxAttempts ? " (giving up)" : "..."));
                }
            }

            log.Add(succeeded
                ? $"[{i}] \"{originalNameForLog}\" -> \"{newName}\" (OK)"
                : $"[{i}] \"{originalNameForLog}\" -> \"{newName}\" FAILED after {maxAttempts} attempts");
        }

        return BuildResult(log, selectedIndices.Count);
    }

    private static List<AutomationElement> GetFreshRows(AutomationElement mainWindow)
    {
        var grid = ZModeler3ScenePanel.FindSceneNodesGrid(mainWindow);
        return grid is null ? new List<AutomationElement>() : ZModeler3ScenePanel.GetRows(grid);
    }

    private static string BuildResult(List<string> log, int total)
    {
        var successCount = log.Count(l => l.EndsWith("(OK)"));
        return $"Renamed {successCount}/{total} row(s):\n{string.Join("\n", log)}";
    }
}
