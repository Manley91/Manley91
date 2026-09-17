namespace ZModeler3LodManager.Core.IO.Json;

/// <summary>
/// Plain data-transfer shape for the lightweight JSON scene format. Meant for testing the LOD
/// logic and for scripting - it has no real geometry, only names and optional triangle counts.
/// </summary>
public class SceneDto
{
    public List<NodeDto> Roots { get; set; } = new();
}

public class NodeDto
{
    public string Name { get; set; } = string.Empty;

    public int? TriangleCount { get; set; }

    public List<NodeDto> Children { get; set; } = new();
}
