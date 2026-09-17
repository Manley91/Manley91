namespace ZModeler3LodManager.Core.Processing;

/// <summary>Commits a previously-computed <see cref="LodPlan"/> to the live scene tree.</summary>
public static class LodPlanApplier
{
    public static void Apply(LodPlan plan)
    {
        foreach (var rename in plan.Renames)
        {
            rename.Node.Name = rename.NewName;
        }

        foreach (var duplication in plan.Duplications)
        {
            var clone = duplication.Source.Clone(duplication.NewName);
            duplication.Parent.AddChild(clone);
        }
    }
}
