namespace ZModeler3LodManager.Core.Naming;

/// <summary>
/// User-configurable settings for how LOD levels are named and how existing variants
/// (already-suffixed meshes, "_high"/"_low", ".001" copies, etc.) are recognized.
/// </summary>
public class LodNamingOptions
{
    /// <summary>How many LOD levels to produce, e.g. 3 for L0..L2.</summary>
    public int LevelCount { get; set; } = 3;

    /// <summary>Suffix appended to the base name for a given level. "{n}" is replaced by the level index.</summary>
    public string SuffixFormat { get; set; } = "_L{n}";

    /// <summary>
    /// Regexes (matched at the end of a name, case-insensitive) that mark a name as "already a
    /// variant of something else". Used to derive a shared base name for grouping, and to strip
    /// stale markers before applying a fresh LOD suffix. Edit this list if a real ZModeler3
    /// export uses a different convention than the defaults below.
    /// </summary>
    public List<string> VariantMarkerPatterns { get; set; } = new(DefaultVariantMarkers);

    public static readonly string[] DefaultVariantMarkers =
    {
        @"_L\d+$",
        @"_LOD\d+$",
        @"_(high|hi|med|medium|mid|low|lo|hp|mp|lp)$",
        @"\.\d{1,3}$",
        @"\s*\(\d+\)$",
        @"_\d+$",
    };
}
