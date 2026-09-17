using ZModeler3LodManager.Core.IO.Json;
using ZModeler3LodManager.Core.IO.Obj;
using ZModeler3LodManager.Core.IO.Z3d;
using ZModeler3LodManager.Core.Models;

namespace ZModeler3LodManager.Core.IO;

/// <summary>Format-detecting facade so the UI doesn't need to know which importer/exporter to call.</summary>
public static class SceneIO
{
    public static SceneFileFormat DetectFormat(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".obj" => SceneFileFormat.Obj,
            ".json" => SceneFileFormat.Json,
            ".z3d" => SceneFileFormat.Z3d,
            _ => throw new NotSupportedException($"Unsupported file extension '{extension}'."),
        };
    }

    public static Scene Import(string path) => DetectFormat(path) switch
    {
        SceneFileFormat.Obj => ObjSceneImporter.Import(path),
        SceneFileFormat.Json => JsonSceneImporter.Import(path),
        SceneFileFormat.Z3d => Z3dSceneImporter.Import(path),
        var format => throw new NotSupportedException($"Unsupported format '{format}'."),
    };

    public static void Export(Scene scene, string path)
    {
        switch (DetectFormat(path))
        {
            case SceneFileFormat.Obj:
                ObjSceneExporter.Export(scene, path);
                break;
            case SceneFileFormat.Json:
                JsonSceneExporter.Export(scene, path);
                break;
            case SceneFileFormat.Z3d:
                Z3dSceneExporter.Export(scene, path);
                break;
            default:
                throw new NotSupportedException("Unsupported format.");
        }
    }
}
