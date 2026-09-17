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

        var selected = new List<AutomationElement>();
        foreach (var row in ZModeler3ScenePanel.GetRows(grid))
        {
            if (IsSelected(row))
            {
                selected.Add(row);
            }
        }

        if (selected.Count == 0)
        {
            return "Nothing is selected in the Scene nodes browser. Select the variants to " +
                   "rename first (top to bottom = highest to lowest detail).";
        }

        var baseName = string.IsNullOrWhiteSpace(baseNameOverride)
            ? LodNaming.GetBaseName(selected[0].Current.Name, options)
            : baseNameOverride!.Trim();

        var (cursorBackX, cursorBackY) = NativeInput.GetCursorPosition();
        var log = new List<string>();

        // Give ZModeler3 generous time to fully open/close its inline editor between rows -
        // firing the next Alt+click before the previous edit has settled is the likely reason
        // only the first row in a multi-row run actually took the new name.
        NativeInput.SendEscape();
        Thread.Sleep(200);

        for (var i = 0; i < selected.Count; i++)
        {
            var row = selected[i];
            var oldName = SafeName(row);
            var newName = LodNaming.BuildLodName(baseName, options, i);

            var rect = row.Current.BoundingRectangle;
            var x = (int)(rect.Left + (rect.Width / 2));
            var y = (int)(rect.Top + (rect.Height / 2));

            NativeInput.AltLeftClick(x, y);
            Thread.Sleep(400);
            NativeInput.MoveCursorTo(cursorBackX, cursorBackY);
            NativeInput.SelectAllTypeAndCommit(newName);
            Thread.Sleep(400);
            NativeInput.SendEscape();
            Thread.Sleep(300);

            log.Add($"[{i}] \"{oldName}\" at ({x},{y}) -> \"{newName}\"");
        }

        return $"Renamed {selected.Count} row(s):\n{string.Join("\n", log)}";
    }

    private static string SafeName(AutomationElement element)
    {
        try
        {
            return element.Current.Name;
        }
        catch
        {
            return "<unavailable>";
        }
    }

    private static bool IsSelected(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObj))
            {
                return ((SelectionItemPattern)patternObj).Current.IsSelected;
            }
        }
        catch
        {
            // Not every element supports this pattern.
        }

        return false;
    }
}
