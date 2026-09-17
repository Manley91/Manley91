using System.Text.Json;
using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.IO.Json;

public static class JsonSceneExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public static void Export(Scene scene, string filePath)
    {
        var dto = new SceneDto
        {
            Roots = scene.Roots.Select(ToDto).ToList(),
        };

        var json = JsonSerializer.Serialize(dto, SerializerOptions);
        File.WriteAllText(filePath, json);
    }

    private static NodeDto ToDto(SceneNode node) => new()
    {
        Name = node.Name,
        TriangleCount = node.TriangleCount,
        Children = node.Children.Select(ToDto).ToList(),
    };
}
