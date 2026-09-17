using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.Processing;

public enum LodMode
{
    /// <summary>Models that already have LOD meshes with different polygon counts: identify and rename them.</summary>
    OrganizeExisting,

    /// <summary>Models without LODs: duplicate each part into the configured number of LOD levels.</summary>
    CreateCopies,
}

public record RenameOperation(SceneNode Node, string OldPath, string OldName, string NewName);

public record DuplicateOperation(SceneNode Source, SceneNode Parent, string SourcePath, string NewName);

/// <summary>
/// A set of planned changes, computed without touching the scene, so the UI can show a preview
/// before the user commits to <see cref="LodPlanApplier.Apply"/>.
/// </summary>
public class LodPlan
{
    public LodMode Mode { get; }

    public List<RenameOperation> Renames { get; } = new();

    public List<DuplicateOperation> Duplications { get; } = new();

    /// <summary>Human-readable notes about parts that were skipped or only partially processed.</summary>
    public List<string> Notes { get; } = new();

    public LodPlan(LodMode mode)
    {
        Mode = mode;
    }

    public bool IsEmpty => Renames.Count == 0 && Duplications.Count == 0;
}
