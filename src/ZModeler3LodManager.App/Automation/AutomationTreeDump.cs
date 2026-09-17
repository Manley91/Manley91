using System.Text;
using System.Windows.Automation;

namespace ZModeler3LodManager.Automation;

/// <summary>
/// Recursively describes an AutomationElement's UI Automation tree so we can figure out, from
/// the outside, where ZModeler3 exposes its hierarchy/scene tree (if at all) and how it can be
/// driven (which patterns each control supports), without ever having seen ZModeler3's source.
/// Diagnostic output only - nothing here changes anything.
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

        var label = $"[{controlType}] \"{name}\"";
        if (automationId.Length > 0)
        {
            label += $" (id={automationId})";
        }

        if (IsSelected(element))
        {
            label += " [SELECTED]";
        }

        var patterns = SupportedPatterns(element);
        if (patterns.Count > 0)
        {
            label += $" patterns=[{string.Join(",", patterns)}]";
        }

        var row = GridRow(element);
        if (row is not null)
        {
            label += $" row={row}";
        }

        return label;
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
            // Not every element supports this pattern - that's fine.
        }

        return false;
    }

    private static int? GridRow(AutomationElement element)
    {
        try
        {
            if (element.TryGetCurrentPattern(GridItemPattern.Pattern, out var patternObj))
            {
                return ((GridItemPattern)patternObj).Current.Row;
            }
        }
        catch
        {
            // Not every element supports this pattern - that's fine.
        }

        return null;
    }

    // Checked explicitly (rather than via GetSupportedPatterns()) so we control exactly which
    // patterns are reported and how - these are the ones that matter for driving ZModeler3:
    // can we read/set text directly (Value), trigger a default action (Invoke), expand/collapse
    // a node, toggle a checkbox, or read rich text (Text)?
    private static List<string> SupportedPatterns(AutomationElement element)
    {
        var found = new List<string>();

        Check(element, ValuePattern.Pattern, "Value", found);
        Check(element, InvokePattern.Pattern, "Invoke", found);
        Check(element, ExpandCollapsePattern.Pattern, "ExpandCollapse", found);
        Check(element, SelectionItemPattern.Pattern, "SelectionItem", found);
        Check(element, GridItemPattern.Pattern, "GridItem", found);
        Check(element, TextPattern.Pattern, "Text", found);
        Check(element, TogglePattern.Pattern, "Toggle", found);
        Check(element, ScrollItemPattern.Pattern, "ScrollItem", found);

        return found;
    }

    private static void Check(AutomationElement element, AutomationPattern pattern, string label, List<string> found)
    {
        try
        {
            if (element.TryGetCurrentPattern(pattern, out _))
            {
                found.Add(label);
            }
        }
        catch
        {
            // Not every element supports this pattern - that's fine.
        }
    }
}
