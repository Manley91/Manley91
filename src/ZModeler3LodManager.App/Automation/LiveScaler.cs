using System.Threading;
using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Live (no-file) uniform scaling of whatever is selected in ZModeler3's Scene nodes browser, by
/// writing the same value into the Properties panel's Scale X/Y/Z fields for each selected node
/// in turn - the click-to-edit/type/verify/retry mechanism already proven for renaming, since the
/// Properties grid is the same kind of custom control (DataItem rows, Text cells with only
/// GridItem pattern, no ValuePattern - no direct way to set a value except clicking into it).
///
/// A botched scale is far more visible than a botched name (it deforms the model), so this
/// selects one node at a time via the safe SelectionItemPattern (no pixel guessing for
/// selection), and verifies each axis individually rather than assuming X succeeding means Y/Z
/// will too.
/// </summary>
public static class LiveScaler
{
    private const int MaxAttempts = 5;

    public static string ScaleSelectedNodes(AutomationElement mainWindow, string scaleValue)
    {
        if (string.IsNullOrWhiteSpace(scaleValue))
        {
            return "Enter a scale value first (e.g. 0.1 or 0.01).";
        }

        var sceneGrid = ZModeler3ScenePanel.FindSceneNodesGrid(mainWindow);
        if (sceneGrid is null)
        {
            return "Couldn't find the Scene nodes browser grid.";
        }

        var initialRows = ZModeler3ScenePanel.GetRows(sceneGrid);
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
            return "Nothing is selected in the Scene nodes browser. Select the node(s) to scale first.";
        }

        var log = new List<string>();
        NativeInput.SendEscape();
        Thread.Sleep(50);

        foreach (var rowIndex in selectedIndices)
        {
            var freshRows = GetFreshSceneRows(mainWindow);
            if (rowIndex >= freshRows.Count)
            {
                log.Add($"[row {rowIndex}] out of range (grid now has {freshRows.Count} rows) - stopping.");
                break;
            }

            var sceneRow = freshRows[rowIndex];
            var nodeName = ZModeler3ScenePanel.SafeName(sceneRow);

            if (!sceneRow.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObj))
            {
                log.Add($"[{nodeName}] row has no SelectionItemPattern - skipping.");
                continue;
            }

            // Make this the sole active node via the standard selection pattern (not a synthetic
            // click) so the Properties panel switches to it - no pixel-coordinate guessing.
            ((SelectionItemPattern)patternObj).Select();
            Thread.Sleep(80);

            var axisResults = new List<string>
            {
                SetAxisValue(mainWindow, "X", scaleValue),
                SetAxisValue(mainWindow, "Y", scaleValue),
                SetAxisValue(mainWindow, "Z", scaleValue),
            };

            log.Add($"[{nodeName}] {string.Join(", ", axisResults)}");
        }

        NativeInput.SendEscape();

        var failures = log.Count(l => l.Contains("FAILED") || l.Contains("skipping") || l.Contains("couldn't find"));
        return $"Scaled {selectedIndices.Count - failures}/{selectedIndices.Count} node(s) to {scaleValue} on X/Y/Z:\n{string.Join("\n", log)}";
    }

    private static string SetAxisValue(AutomationElement mainWindow, string axisName, string newValue)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var valueCell = FindScaleAxisValueCell(mainWindow, axisName);
            if (valueCell is null)
            {
                return $"{axisName}: couldn't find the Scale {axisName} row in Properties";
            }

            var currentText = ZModeler3ScenePanel.SafeName(valueCell);
            if (currentText == newValue)
            {
                return $"{axisName}: OK ({newValue})";
            }

            var rect = valueCell.Current.BoundingRectangle;
            var x = (int)(rect.Left + (rect.Width / 2));
            var y = (int)(rect.Top + (rect.Height / 2));

            NativeInput.SpamLeftClicks(x, y);
            Thread.Sleep(60);
            NativeInput.SelectAllTypeAndCommit(newValue);
            Thread.Sleep(60);
            NativeInput.SendEscape();
            Thread.Sleep(40);

            var verifyCell = FindScaleAxisValueCell(mainWindow, axisName);
            if (verifyCell is not null && ZModeler3ScenePanel.SafeName(verifyCell) == newValue)
            {
                return $"{axisName}: {currentText} -> {newValue} (OK)";
            }
        }

        return $"{axisName}: FAILED after {MaxAttempts} attempts";
    }

    /// <summary>The "Scale" row is a category header immediately followed by its X/Y/Z rows (same
    /// flat sibling layout the Scene nodes browser uses for LOD groups) - found by name each time
    /// rather than a fixed row number, since the row it lands on shifts with the node's property
    /// count (a Dummy helper has fewer rows above Transformation than a mesh does).</summary>
    private static AutomationElement? FindScaleAxisValueCell(AutomationElement mainWindow, string axisName)
    {
        var grid = ZModeler3PropertiesPanel.FindPropertiesGrid(mainWindow);
        if (grid is null)
        {
            return null;
        }

        var rows = ZModeler3PropertiesPanel.GetRows(grid);
        var scaleIndex = rows.FindIndex(r => ZModeler3ScenePanel.SafeName(r) == "Scale");
        if (scaleIndex < 0)
        {
            return null;
        }

        var offset = axisName switch { "X" => 1, "Y" => 2, "Z" => 3, _ => -1 };
        var axisRowIndex = scaleIndex + offset;
        if (offset < 0 || axisRowIndex >= rows.Count || ZModeler3ScenePanel.SafeName(rows[axisRowIndex]) != axisName)
        {
            return null;
        }

        return ZModeler3PropertiesPanel.GetValueCell(rows[axisRowIndex]);
    }

    private static List<AutomationElement> GetFreshSceneRows(AutomationElement mainWindow)
    {
        var grid = ZModeler3ScenePanel.FindSceneNodesGrid(mainWindow);
        return grid is null ? new List<AutomationElement>() : ZModeler3ScenePanel.GetRows(grid);
    }
}
