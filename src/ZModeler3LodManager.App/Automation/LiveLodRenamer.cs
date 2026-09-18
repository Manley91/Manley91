using System.Threading;
using System.Windows.Automation;
using ZModeler3LodManager.Core.Naming;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Live (no-file) renaming of whatever is currently selected in ZModeler3's Scene nodes
/// browser, in top-to-bottom order - no export/import round trip needed.
/// </summary>
public static class LiveLodRenamer
{
    private const int MaxAttempts = 5;

    /// <summary>
    /// "Organize Existing LODs": numbering resets every LevelCount selected items, so selecting
    /// several groups at once (e.g. 3 parts, 3 variants each) gives each its own L0..L(n-1)
    /// instead of one sequence numbered straight through. Each group's base name is derived
    /// fresh from its own first (still-unrenamed) member, unless overridden.
    /// </summary>
    public static string RenameSelectedAsLodLevels(AutomationElement mainWindow, LodNamingOptions options, string? baseNameOverride)
    {
        var groupBaseName = baseNameOverride?.Trim() ?? string.Empty;

        return RenameSelectedRows(mainWindow, (i, rowIndex) =>
        {
            var level = i % options.LevelCount;
            if (level == 0 && string.IsNullOrWhiteSpace(baseNameOverride))
            {
                var firstInGroup = GetFreshRows(mainWindow).ElementAtOrDefault(rowIndex);
                groupBaseName = LodNaming.GetBaseName(
                    firstInGroup is null ? string.Empty : ZModeler3ScenePanel.SafeName(firstInGroup), options);
            }

            return (LodNaming.BuildLodName(groupBaseName, options, level), $"group {i / options.LevelCount} L{level}");
        });
    }

    /// <summary>"Auto rename sirens": every selected row, in order, becomes "{baseName}{n}"
    /// starting at <paramref name="startNumber"/> (1 for siren1..siren32) - one flat sequence
    /// across the whole selection, not grouped.</summary>
    public static string RenameSelectedSequentially(AutomationElement mainWindow, string baseName, int startNumber)
    {
        return RenameSelectedRows(mainWindow, (i, _) => ($"{baseName}{startNumber + i}", $"#{startNumber + i}"));
    }

    /// <summary>
    /// Shared click/type/verify/retry loop. <paramref name="buildName"/> receives the selection
    /// index (0-based) and the row's absolute grid position, and returns the desired new name
    /// plus a short label for the log.
    /// </summary>
    private static string RenameSelectedRows(AutomationElement mainWindow, Func<int, int, (string NewName, string Label)> buildName)
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
            return "Nothing is selected in the Scene nodes browser. Select the rows to rename first (top to bottom).";
        }

        var log = new List<string>();

        NativeInput.SendEscape();
        Thread.Sleep(50);

        // Target rows by their ABSOLUTE position in the grid, fixed up front - not by name
        // (duplicate names all resolve to the same first match) and not by "currently selected"
        // (renaming a row drops the rest of the original multi-selection, so re-querying
        // "selected rows" shrinks after every step). Position is stable because renaming a row
        // doesn't reorder the grid.
        for (var i = 0; i < selectedIndices.Count; i++)
        {
            var rowIndex = selectedIndices[i];
            var (newName, label) = buildName(i, rowIndex);
            var originalNameForLog = "?";
            var succeeded = false;

            for (var attempt = 1; attempt <= MaxAttempts && !succeeded; attempt++)
            {
                var freshRows = GetFreshRows(mainWindow);
                if (rowIndex >= freshRows.Count)
                {
                    log.Add($"[{label}] row index {rowIndex} is out of range (grid now has {freshRows.Count} rows) - stopping.");
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

                NativeInput.SpamLeftClicks(x, y);
                Thread.Sleep(60);
                NativeInput.SelectAllTypeAndCommit(newName);
                Thread.Sleep(60);
                NativeInput.SendEscape();
                Thread.Sleep(40);

                var verifyRows = GetFreshRows(mainWindow);
                succeeded = rowIndex < verifyRows.Count && ZModeler3ScenePanel.SafeName(verifyRows[rowIndex]) == newName;

                if (!succeeded)
                {
                    log.Add($"[{label}] attempt {attempt} at ({x},{y}) didn't take - retrying" +
                            (attempt == MaxAttempts ? " (giving up)" : "..."));
                }
            }

            log.Add(succeeded
                ? $"[{label}] \"{originalNameForLog}\" -> \"{newName}\" (OK)"
                : $"[{label}] \"{originalNameForLog}\" -> \"{newName}\" FAILED after {MaxAttempts} attempts");
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
