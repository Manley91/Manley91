using ZModeler3LodManager.Core.Models;
using ZModeler3LodManager.Core.Naming;

namespace ZModeler3LodManager.Core.Processing;

public static class LodGrouping
{
    /// <summary>
    /// Groups every leaf descendant under <paramref name="root"/>'s children by base name.
    /// Works both for flat siblings (Bumper_high, Bumper_low sitting next to each other, as a
    /// flat OBJ export would produce) and for LODs nested one level under a part
    /// (Bumper -> [Bumper_high, Bumper_low]) - either way the leaves end up grouped together.
    /// </summary>
    public static List<VariantGroup> GroupDescendantsByBaseName(SceneNode root, LodNamingOptions options)
    {
        var leaves = root.Children
            .SelectMany(child => child.DescendantsAndSelf())
            .Where(node => node.Children.Count == 0);

        return leaves
            .GroupBy(node => LodNaming.GetBaseName(node.Name, options), StringComparer.OrdinalIgnoreCase)
            .Select(group => new VariantGroup(group.Key, group.ToList()))
            .ToList();
    }

    /// <summary>Groups only the direct children of <paramref name="root"/> by base name.</summary>
    public static List<VariantGroup> GroupDirectChildren(SceneNode root, LodNamingOptions options)
    {
        return root.Children
            .GroupBy(node => LodNaming.GetBaseName(node.Name, options), StringComparer.OrdinalIgnoreCase)
            .Select(group => new VariantGroup(group.Key, group.ToList()))
            .ToList();
    }
}
