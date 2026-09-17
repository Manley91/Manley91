using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.Processing;

/// <summary>A set of nodes that share a base name, i.e. candidate LOD variants of the same part.</summary>
public record VariantGroup(string BaseName, List<SceneNode> Members);
