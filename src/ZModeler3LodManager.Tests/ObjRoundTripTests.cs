using ZModeler3LodManager.Core.IO;
using ZModeler3LodManager.Core.IO.Obj;
using ZModeler3LodManager.Core.Naming;
using ZModeler3LodManager.Core.Processing;
using Xunit;

namespace ZModeler3LodManager.Tests;

public class ObjRoundTripTests
{
    private static string SamplePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Samples", fileName);

    [Fact]
    public void Import_PoliceCarWithNoLods_ProducesOneRootWithSixFlatParts()
    {
        var scene = ObjSceneImporter.Import(SamplePath("police_car_no_lods.obj"));

        var root = Assert.Single(scene.Roots);
        Assert.Equal(6, root.Children.Count);
        Assert.Contains(root.Children, c => c.Name == "Body" && c.TriangleCount == 48);
        Assert.NotNull(scene.ObjHeaderLines);
        Assert.Contains(scene.ObjHeaderLines!, l => l.StartsWith("v "));
    }

    [Fact]
    public void CreateLodCopies_OnImportedObj_DuplicatesFacesWithoutTouchingVertexPool()
    {
        var scene = ObjSceneImporter.Import(SamplePath("police_car_no_lods.obj"));
        var root = scene.Roots[0];
        var originalHeaderLineCount = scene.ObjHeaderLines!.Count;
        var originalBodyFaceLines = root.Children.Single(c => c.Name == "Body").RawGeometryLines!.Count;

        var plan = LodPlanner.PlanCreateCopies(new[] { root }, new LodNamingOptions { LevelCount = 3 });
        LodPlanApplier.Apply(plan);

        var bodyLevels = root.Children.Where(c => c.Name.StartsWith("Body_L")).ToList();
        Assert.Equal(3, bodyLevels.Count);
        Assert.All(bodyLevels, c => Assert.Equal(originalBodyFaceLines, c.RawGeometryLines!.Count));

        var outPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".obj");
        try
        {
            ObjSceneExporter.Export(scene, outPath);
            var exportedLines = File.ReadAllLines(outPath);

            // Vertex pool must be untouched: same count of "v " lines, byte-for-byte.
            var exportedVertexLines = exportedLines.Where(l => l.StartsWith("v ")).ToList();
            var originalVertexLines = scene.ObjHeaderLines!.Where(l => l.StartsWith("v ")).ToList();
            Assert.Equal(originalVertexLines, exportedVertexLines);
            Assert.Equal(originalHeaderLineCount, scene.ObjHeaderLines!.Count);

            Assert.Contains(exportedLines, l => l == "o police_car_no_lods/Body_L0");
            Assert.Contains(exportedLines, l => l == "o police_car_no_lods/Body_L1");
            Assert.Contains(exportedLines, l => l == "o police_car_no_lods/Body_L2");
        }
        finally
        {
            File.Delete(outPath);
        }
    }

    [Fact]
    public void OrganizeExisting_OnMixedLodTruck_GroupsAndRenamesByTriangleCount()
    {
        var scene = ObjSceneImporter.Import(SamplePath("truck_mixed_lods.obj"));
        var root = scene.Roots[0];

        var plan = LodPlanner.PlanOrganizeExisting(new[] { root }, new LodNamingOptions { LevelCount = 3 });
        LodPlanApplier.Apply(plan);

        Assert.Contains(root.Children, c => c.Name == "Cab_L0" && c.TriangleCount == 192);
        Assert.Contains(root.Children, c => c.Name == "Cab_L1" && c.TriangleCount == 12);
        Assert.Contains(root.Children, c => c.Name == "Chassis_L0" && c.TriangleCount == 108);
        Assert.Contains(root.Children, c => c.Name == "Chassis_L1" && c.TriangleCount == 12);
        Assert.Contains(root.Children, c => c.Name == "Bumper_L0" && c.TriangleCount == 48);
        Assert.Contains(root.Children, c => c.Name == "Bumper_L1" && c.TriangleCount == 12);
        // Grille had only one variant: left completely untouched.
        Assert.Contains(root.Children, c => c.Name == "Grille");
        Assert.DoesNotContain(root.Children, c => c.Name is "Grille_L0" or "Grille_L1");
    }

    [Fact]
    public void SceneIO_DetectsFormatFromExtension()
    {
        Assert.Equal(SceneFileFormat.Obj, SceneIO.DetectFormat("model.obj"));
        Assert.Equal(SceneFileFormat.Json, SceneIO.DetectFormat("model.json"));
        Assert.Equal(SceneFileFormat.Z3d, SceneIO.DetectFormat("model.z3d"));
    }

    [Fact]
    public void SceneIO_Z3d_ThrowsAClearNotSupportedError()
    {
        var ex = Assert.Throws<NotSupportedException>(() => SceneIO.Import("model.z3d"));
        Assert.Contains("proprietary", ex.Message);
    }
}
