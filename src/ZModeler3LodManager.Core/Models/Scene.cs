namespace ZModeler3LodManager.Core.Models;

/// <summary>
/// A loaded model file: one or more root hierarchies plus any format-specific data that
/// needs to be carried through unchanged on export (e.g. an OBJ file's v/vt/vn/mtllib lines).
/// </summary>
public class Scene
{
    public string SourcePath { get; set; } = string.Empty;

    public List<SceneNode> Roots { get; } = new();

    /// <summary>
    /// Populated by <see cref="IO.Obj.ObjSceneImporter"/> with the file's vertex/normal/texcoord
    /// and other non-object lines, kept verbatim so <see cref="IO.Obj.ObjSceneExporter"/> can
    /// re-emit them unchanged. Geometry is never regenerated or guessed.
    /// </summary>
    public List<string>? ObjHeaderLines { get; set; }

    public IEnumerable<SceneNode> AllNodes() => Roots.SelectMany(r => r.DescendantsAndSelf());
}
