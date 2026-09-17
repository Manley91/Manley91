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
        if (initialSelected.Count == 0)
        {
            return "Nothing is selected in the Scene nodes browser. Select the variants to " +
                   "rename first (top to bottom = highest to lowest detail).";
        }

        // Capture original names up front - everything after this re-finds each row fresh by
        // this name right before touching it, rather than reusing AutomationElement references
        // across renames. Reusing them was the bug: after the first row's commit, ZModeler3
        // appears to rebuild its row elements, silently staling out any references we held onto
        // for the rest of the selection, which is why only the first row ever actually changed.
        var originalNames = new List<string>();
        foreach (var row in initialSelected)
        {
            originalNames.Add(ZModeler3ScenePanel.SafeName(row));
        }

        var baseName = string.IsNullOrWhiteSpace(baseNameOverride)
            ? LodNaming.GetBaseName(originalNames[0], options)
            : baseNameOverride!.Trim();

        var log = new List<string>();
        const int maxAttempts = 5;

        NativeInput.SendEscape();
        Thread.Sleep(250);

        for (var i = 0; i < originalNames.Count; i++)
        {
            var originalName = originalNames[i];
            var newName = LodNaming.BuildLodName(baseName, options, i);
            var succeeded = false;

            for (var attempt = 1; attempt <= maxAttempts && !succeeded; attempt++)
            {
                var target = ZModeler3ScenePanel.FindRowByName(mainWindow, originalName);
                if (target is null)
                {
                    log.Add($"[{i}] couldn't re-find a row named \"{originalName}\" (already renamed by an earlier step, or duplicate names in the selection?) - skipping.");
                    break;
                }

                var rect = target.Current.BoundingRectangle;
                var x = (int)(rect.Left + (rect.Width / 2));
                var y = (int)(rect.Top + (rect.Height / 2));

                NativeInput.AltLeftClick(x, y);
                Thread.Sleep(450);
                NativeInput.SelectAllTypeAndCommit(newName);
                Thread.Sleep(450);
                NativeInput.SendEscape();
                Thread.Sleep(300);

                succeeded = ZModeler3ScenePanel.FindRowByName(mainWindow, newName) is not null;

                if (!succeeded)
                {
                    log.Add($"[{i}] attempt {attempt} at ({x},{y}) didn't take - retrying" +
                            (attempt == maxAttempts ? " (giving up)" : "..."));
                }
            }

            log.Add(succeeded
                ? $"[{i}] \"{originalName}\" -> \"{newName}\" (OK)"
                : $"[{i}] \"{originalName}\" -> \"{newName}\" FAILED after {maxAttempts} attempts");
        }

        var successCount = log.Count(l => l.EndsWith("(OK)"));
        return $"Renamed {successCount}/{originalNames.Count} row(s):\n{string.Join("\n", log)}";
    }
}
