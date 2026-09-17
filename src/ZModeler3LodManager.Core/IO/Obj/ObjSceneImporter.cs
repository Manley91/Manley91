using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.IO.Obj;

/// <summary>
/// Imports a Wavefront .obj file. OBJ has no real hierarchy, so every "o"/"g" object becomes a
/// flat child of one synthetic root node named after the file. Vertex/normal/texcoord lines are
/// kept verbatim on <see cref="Scene.ObjHeaderLines"/> and never touched, and each object's
/// face lines are kept verbatim too - nothing about the geometry is parsed, reduced or guessed.
/// </summary>
public static class ObjSceneImporter
{
    public static Scene Import(string filePath)
    {
        var scene = new Scene { SourcePath = filePath };
        var root = new SceneNode(System.IO.Path.GetFileNameWithoutExtension(filePath));
        scene.Roots.Add(root);

        var header = new List<string>();
        SceneNode? current = null;
        var currentLines = new List<string>();
        var currentFaceCount = 0;

        void FlushCurrent()
        {
            if (current is null)
            {
                return;
            }

            current.RawGeometryLines = new List<string>(currentLines);
            current.TriangleCount = currentFaceCount;
        }

        foreach (var rawLine in File.ReadLines(filePath))
        {
            var line = rawLine.TrimEnd('\r', '\n');
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith("o ", StringComparison.Ordinal) || trimmed.StartsWith("g ", StringComparison.Ordinal))
            {
                FlushCurrent();

                var name = trimmed[2..].Trim();
                if (name.Length == 0)
                {
                    name = $"group_{root.Children.Count + 1}";
                }

                current = new SceneNode(name);
                root.AddChild(current);
                currentLines = new List<string>();
                currentFaceCount = 0;
                continue;
            }

            var isVertexOrHeaderLine =
                trimmed.StartsWith("v ", StringComparison.Ordinal) ||
                trimmed.StartsWith("vt ", StringComparison.Ordinal) ||
                trimmed.StartsWith("vn ", StringComparison.Ordinal) ||
                trimmed.StartsWith("vp ", StringComparison.Ordinal) ||
                trimmed.StartsWith("mtllib", StringComparison.Ordinal) ||
                trimmed.StartsWith("#", StringComparison.Ordinal) ||
                trimmed.Length == 0;

            if (isVertexOrHeaderLine)
            {
                header.Add(line);
                continue;
            }

            // A face/material/smoothing line before any "o"/"g" marker: the file has a single
            // unnamed object, so create it lazily.
            current ??= CreateDefaultObject(root);
            currentLines.Add(line);
            if (trimmed.StartsWith("f ", StringComparison.Ordinal))
            {
                currentFaceCount++;
            }
        }

        FlushCurrent();
        scene.ObjHeaderLines = header;
        return scene;
    }

    private static SceneNode CreateDefaultObject(SceneNode root)
    {
        var node = new SceneNode(root.Name);
        root.AddChild(node);
        return node;
    }
}
