using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

/// <summary>Locates the "Scene nodes browser" panel and its rows within a ZModeler3 window.</summary>
public static class ZModeler3ScenePanel
{
    public static AutomationElement? FindSceneNodesGrid(AutomationElement mainWindow)
    {
        var panel = mainWindow.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, "Scene nodes browser"));

        return panel?.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataGrid));
    }

    public static List<AutomationElement> GetRows(AutomationElement grid)
    {
        var rows = grid.FindAll(
            TreeScope.Children,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.DataItem));

        var result = new List<AutomationElement>();
        foreach (AutomationElement row in rows)
        {
            result.Add(row);
        }

        return result;
    }

    /// <summary>Re-finds the grid and returns its currently selected rows, freshly queried -
    /// never reuse an AutomationElement captured before a rename, ZModeler3 appears to rebuild
    /// its row elements on commit, which silently stales out old references.</summary>
    public static List<AutomationElement> GetFreshSelectedRows(AutomationElement mainWindow)
    {
        var grid = FindSceneNodesGrid(mainWindow);
        if (grid is null)
        {
            return new List<AutomationElement>();
        }

        var result = new List<AutomationElement>();
        foreach (var row in GetRows(grid))
        {
            if (IsSelected(row))
            {
                result.Add(row);
            }
        }

        return result;
    }

    public static string SafeName(AutomationElement element)
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

    public static bool IsSelected(AutomationElement element)
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
