using System.Text.Json;
using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.IO.Json;

public static class JsonSceneImporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static Scene Import(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var dto = JsonSerializer.Deserialize<SceneDto>(json, SerializerOptions)
                  ?? throw new InvalidDataException($"'{filePath}' does not contain a valid scene.");

        var scene = new Scene { SourcePath = filePath };
        foreach (var rootDto in dto.Roots)
        {
            scene.Roots.Add(ToSceneNode(rootDto));
        }

        return scene;
    }

    private static SceneNode ToSceneNode(NodeDto dto)
    {
        var node = new SceneNode(dto.Name) { TriangleCount = dto.TriangleCount };
        foreach (var childDto in dto.Children)
        {
            node.AddChild(ToSceneNode(childDto));
        }

        return node;
    }
}
