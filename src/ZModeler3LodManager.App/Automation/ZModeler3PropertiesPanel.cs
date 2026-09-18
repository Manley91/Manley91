using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

/// <summary>Locates the Properties grid (AutomationId "1002") and its rows/value cells within a
/// ZModeler3 window - the same kind of custom two-column grid as the Scene nodes browser, just
/// with a Property-name column and a Value column instead of one Name column.</summary>
public static class ZModeler3PropertiesPanel
{
    public static AutomationElement? FindPropertiesGrid(AutomationElement mainWindow)
    {
        return mainWindow.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, "1002"));
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

    /// <summary>Each row has two Text children at fixed columns - a property-name label (left)
    /// and the actual value (right). The row's own bounding rectangle spans both columns, so
    /// clicking its center lands on the label, not the value - this returns the value cell
    /// specifically (the rightmost of the two Text children).</summary>
    public static AutomationElement? GetValueCell(AutomationElement row)
    {
        var texts = row.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

        AutomationElement? valueCell = null;
        var rightmostLeft = double.MinValue;
        foreach (AutomationElement text in texts)
        {
            var left = text.Current.BoundingRectangle.Left;
            if (left > rightmostLeft)
            {
                rightmostLeft = left;
                valueCell = text;
            }
        }

        return valueCell;
    }
}
