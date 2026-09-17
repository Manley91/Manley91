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
}
