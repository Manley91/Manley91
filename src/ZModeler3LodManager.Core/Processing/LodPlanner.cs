using ZModeler3LodManager.Core.Models;
using ZModeler3LodManager.Core.Naming;

namespace ZModeler3LodManager.Core.Processing;

/// <summary>
/// Computes what Organize Existing LODs / Create LOD Copies would do to a set of selected
/// hierarchies, without mutating anything. Call <see cref="LodPlanApplier.Apply"/> on the
/// result once the user has reviewed the preview.
/// </summary>
public static class LodPlanner
{
    /// <summary>
    /// Mode 1: for each selected hierarchy, groups existing leaf meshes by base name and
    /// renames the members of each multi-member group to L0..L(n-1) ordered by triangle count
    /// (highest detail first). Groups with only one member are left untouched - there is
    /// nothing to organize.
    /// </summary>
    public static LodPlan PlanOrganizeExisting(IEnumerable<SceneNode> selectedRoots, LodNamingOptions options)
    {
        var plan = new LodPlan(LodMode.OrganizeExisting);

        foreach (var root in selectedRoots)
        {
            var groups = LodGrouping.GroupDescendantsByBaseName(root, options);

            foreach (var group in groups)
            {
                if (group.Members.Count < 2)
                {
                    plan.Notes.Add(
                        $"\"{group.BaseName}\" under \"{root.Name}\": only one variant found, nothing to organize (skipped).");
                    continue;
                }

                var ordered = group.Members
                    .OrderByDescending(m => m.TriangleCount ?? -1)
                    .ToList();

                var levelsToAssign = Math.Min(options.LevelCount, ordered.Count);
                for (var level = 0; level < levelsToAssign; level++)
                {
                    var node = ordered[level];
                    var newName = LodNaming.BuildLodName(group.BaseName, options, level);
                    if (node.Name != newName)
                    {
                        plan.Renames.Add(new RenameOperation(node, node.Path, node.Name, newName));
                    }
                }

                if (ordered.Count > options.LevelCount)
                {
                    plan.Notes.Add(
                        $"\"{group.BaseName}\" under \"{root.Name}\": {ordered.Count} variants found but only " +
                        $"{options.LevelCount} level(s) configured; {ordered.Count - options.LevelCount} " +
                        "lowest-detail variant(s) left unrenamed.");
                }
                else if (ordered.Count < options.LevelCount)
                {
                    plan.Notes.Add(
                        $"\"{group.BaseName}\" under \"{root.Name}\": only {ordered.Count} of " +
                        $"{options.LevelCount} configured level(s) present.");
                }
            }
        }

        return plan;
    }

    /// <summary>
    /// Mode 2: for each selected hierarchy, duplicates every direct child that has no existing
    /// LOD variants (anywhere in its own subtree) into the configured number of LOD levels,
    /// renaming the original plus the copies to L0..L(n-1). Parts that already have variants
    /// are skipped with a note pointing at Organize Existing LODs instead.
    /// </summary>
    public static LodPlan PlanCreateCopies(IEnumerable<SceneNode> selectedRoots, LodNamingOptions options)
    {
        var plan = new LodPlan(LodMode.CreateCopies);

        foreach (var root in selectedRoots)
        {
            var deepGroups = LodGrouping.GroupDescendantsByBaseName(root, options);
            var alreadyHasLods = new HashSet<SceneNode>(
                deepGroups.Where(g => g.Members.Count > 1).SelectMany(g => g.Members));

            foreach (var child in root.Children)
            {
                if (child.DescendantsAndSelf().Any(alreadyHasLods.Contains))
                {
                    plan.Notes.Add(
                        $"\"{child.Name}\" under \"{root.Name}\": already has LOD variants, skipped " +
                        "(use Organize Existing LODs instead).");
                    continue;
                }

                var baseName = LodNaming.GetBaseName(child.Name, options);

                var renamedOriginal = LodNaming.BuildLodName(baseName, options, 0);
                if (child.Name != renamedOriginal)
                {
                    plan.Renames.Add(new RenameOperation(child, child.Path, child.Name, renamedOriginal));
                }

                for (var level = 1; level < options.LevelCount; level++)
                {
                    var newName = LodNaming.BuildLodName(baseName, options, level);
                    plan.Duplications.Add(new DuplicateOperation(child, root, child.Path, newName));
                }
            }
        }

        return plan;
    }
}
