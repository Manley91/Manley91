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
        var initialSelected = ZModeler3ScenePanel.GetFreshSelectedRows(mainWindow);
        var count = initialSelected.Count;
        if (count == 0)
        {
            return "Nothing is selected in the Scene nodes browser. Select the variants to " +
                   "rename first (top to bottom = highest to lowest detail).";
        }

        var baseName = string.IsNullOrWhiteSpace(baseNameOverride)
            ? LodNaming.GetBaseName(ZModeler3ScenePanel.SafeName(initialSelected[0]), options)
            : baseNameOverride!.Trim();

        var log = new List<string>();
        const int maxAttempts = 5;

        NativeInput.SendEscape();
        Thread.Sleep(250);

        // Identify each target by its POSITION in the freshly-queried selection, re-queried
        // right before every attempt - not by name (duplicate names, e.g. several unrenamed
        // "Box" copies, would all resolve to the same first match) and not by holding onto an
        // AutomationElement across renames (ZModeler3 appears to rebuild row elements on
        // commit, staling out old references).
        for (var i = 0; i < count; i++)
        {
            var newName = LodNaming.BuildLodName(baseName, options, i);
            var originalNameForLog = "?";
            var succeeded = false;

            for (var attempt = 1; attempt <= maxAttempts && !succeeded; attempt++)
            {
                var freshSelected = ZModeler3ScenePanel.GetFreshSelectedRows(mainWindow);
                if (i >= freshSelected.Count)
                {
                    log.Add($"[{i}] expected at least {i + 1} selected row(s) but only found {freshSelected.Count} - stopping.");
                    return BuildResult(log, count);
                }

                var target = freshSelected[i];
                originalNameForLog = ZModeler3ScenePanel.SafeName(target);

                if (originalNameForLog == newName)
                {
                    succeeded = true;
                    break;
                }

                var rect = target.Current.BoundingRectangle;
                var x = (int)(rect.Left + (rect.Width / 2));
                var y = (int)(rect.Top + (rect.Height / 2));

                NativeInput.DoubleLeftClick(x, y);
                Thread.Sleep(450);
                NativeInput.SelectAllTypeAndCommit(newName);
                Thread.Sleep(450);
                NativeInput.SendEscape();
                Thread.Sleep(300);

                var verifySelected = ZModeler3ScenePanel.GetFreshSelectedRows(mainWindow);
                succeeded = i < verifySelected.Count && ZModeler3ScenePanel.SafeName(verifySelected[i]) == newName;

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

        return BuildResult(log, count);
    }

    private static string BuildResult(List<string> log, int total)
    {
        var successCount = log.Count(l => l.EndsWith("(OK)"));
        return $"Renamed {successCount}/{total} row(s):\n{string.Join("\n", log)}";
    }
}
