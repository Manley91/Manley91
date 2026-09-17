namespace ZModeler3LodManager.Core.Models;

/// <summary>
/// One node in a model's hierarchy (a "part" such as Body, Wheels, or a single LOD mesh).
/// </summary>
public class SceneNode
{
    public string Name { get; set; }

    public SceneNode? Parent { get; private set; }

    public List<SceneNode> Children { get; } = new();

    /// <summary>Triangle/face count for this node's own geometry, if known.</summary>
    public int? TriangleCount { get; set; }

    /// <summary>
    /// Raw OBJ lines (f / usemtl / s) for this node's geometry, verbatim, referencing the
    /// shared vertex pool stored on the owning <see cref="Scene"/>. Null for nodes that carry
    /// no geometry of their own (pure grouping nodes) or that came from a format without
    /// real geometry (e.g. the JSON test format).
    /// </summary>
    public List<string>? RawGeometryLines { get; set; }

    public SceneNode(string name)
    {
        Name = name;
    }

    public void AddChild(SceneNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void RemoveChild(SceneNode child)
    {
        if (Children.Remove(child))
        {
            child.Parent = null;
        }
    }

    /// <summary>Deep-clones this node and its whole subtree under a new name.</summary>
    public SceneNode Clone(string newName)
    {
        var clone = new SceneNode(newName)
        {
            TriangleCount = TriangleCount,
            RawGeometryLines = RawGeometryLines is null ? null : new List<string>(RawGeometryLines),
        };
        foreach (var child in Children)
        {
            clone.AddChild(child.Clone(child.Name));
        }
        return clone;
    }

    public IEnumerable<SceneNode> DescendantsAndSelf()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var descendant in child.DescendantsAndSelf())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>Slash-separated path from the root, used for display and for OBJ object names.</summary>
    public string Path => Parent is null ? Name : $"{Parent.Path}/{Name}";

    public override string ToString() => Name;
}
