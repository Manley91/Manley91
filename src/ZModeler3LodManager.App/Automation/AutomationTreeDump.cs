using System.Text;
using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Recursively describes an AutomationElement's UI Automation tree so we can figure out, from
/// the outside, where ZModeler3 exposes its hierarchy/scene tree (if at all) without ever having
/// seen ZModeler3's source. Diagnostic output only - nothing here changes anything.
/// </summary>
public static class AutomationTreeDump
{
    public static string Dump(AutomationElement root, int maxDepth = 6, int maxChildrenPerNode = 60)
    {
        var builder = new StringBuilder();
        Walk(root, 0, maxDepth, maxChildrenPerNode, builder);
        return builder.ToString();
    }

    private static void Walk(AutomationElement element, int depth, int maxDepth, int maxChildrenPerNode, StringBuilder builder)
    {
        builder.Append(' ', depth * 2);
        builder.AppendLine(Describe(element));

        if (depth >= maxDepth)
        {
            return;
        }

        AutomationElementCollection children;
        try
        {
            children = element.FindAll(TreeScope.Children, Condition.TrueCondition);
        }
        catch (Exception ex)
        {
            builder.Append(' ', (depth + 1) * 2);
            builder.AppendLine($"[couldn't read children: {ex.Message}]");
            return;
        }

        var shown = 0;
        foreach (AutomationElement child in children)
        {
            if (shown >= maxChildrenPerNode)
            {
                builder.Append(' ', (depth + 1) * 2);
                builder.AppendLine($"... ({children.Count - maxChildrenPerNode} more children not shown)");
                break;
            }

            Walk(child, depth + 1, maxDepth, maxChildrenPerNode, builder);
            shown++;
        }
    }

    private static string Describe(AutomationElement element)
    {
        string name;
        string controlType;
        string automationId;
        var isSelected = false;

        try
        {
            name = element.Current.Name;
        }
        catch
        {
            name = "<name unavailable>";
        }

        try
        {
            controlType = element.Current.ControlType.ProgrammaticName;
        }
        catch
        {
            controlType = "<type unavailable>";
        }

        try
        {
            automationId = element.Current.AutomationId;
        }
        catch
        {
            automationId = "";
        }

        try
        {
            if (element.TryGetCurrentPattern(SelectionItemPattern.Pattern, out var patternObj))
            {
                isSelected = ((SelectionItemPattern)patternObj).Current.IsSelected;
            }
        }
        catch
        {
            // Not every element supports this pattern - that's fine, just skip it.
        }

        var label = $"[{controlType}] \"{name}\"";
        if (automationId.Length > 0)
        {
            label += $" (id={automationId})";
        }

        if (isSelected)
        {
            label += " [SELECTED]";
        }

        return label;
    }
}
