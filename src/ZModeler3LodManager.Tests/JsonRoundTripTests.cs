using ZModeler3LodManager.Core.IO.Json;
using ZModeler3LodManager.Core.Naming;
using ZModeler3LodManager.Core.Processing;
using Xunit;

namespace ZModeler3LodManager.Tests;

public class JsonRoundTripTests
{
    private static string SamplePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Samples", fileName);

    [Fact]
    public void Import_PoliceCarJson_MatchesExpectedHierarchy()
    {
        var scene = JsonSceneImporter.Import(SamplePath("police_car.json"));

        var root = Assert.Single(scene.Roots);
        Assert.Equal("PoliceCar", root.Name);
        Assert.Equal(6, root.Children.Count);
        Assert.Equal(4200, root.Children.Single(c => c.Name == "Body").TriangleCount);
    }

    [Fact]
    public void ExportThenImport_PreservesNamesAndTriangleCounts()
    {
        var scene = JsonSceneImporter.Import(SamplePath("police_car.json"));
        var outPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");

        try
        {
            JsonSceneExporter.Export(scene, outPath);
            var reimported = JsonSceneImporter.Import(outPath);

            var original = scene.Roots[0].Children.Select(c => (c.Name, c.TriangleCount)).ToList();
            var roundTripped = reimported.Roots[0].Children.Select(c => (c.Name, c.TriangleCount)).ToList();
            Assert.Equal(original, roundTripped);
        }
        finally
        {
            File.Delete(outPath);
        }
    }

    [Fact]
    public void OrganizeExisting_OnNestedLodJson_FindsVariantsNestedUnderAPart()
    {
        var scene = JsonSceneImporter.Import(SamplePath("fire_truck_nested_lods.json"));
        var root = scene.Roots[0];

        var plan = LodPlanner.PlanOrganizeExisting(new[] { root }, new LodNamingOptions { LevelCount = 3 });
        LodPlanApplier.Apply(plan);

        var body = root.Children.Single(c => c.Name == "Body");
        Assert.Contains(body.Children, c => c.Name == "Body_L0" && c.TriangleCount == 5200);
        Assert.Contains(body.Children, c => c.Name == "Body_L1" && c.TriangleCount == 2600);
        Assert.Contains(body.Children, c => c.Name == "Body_L2" && c.TriangleCount == 900);

        var ladder = root.Children.Single(c => c.Name == "Ladder");
        Assert.Contains(ladder.Children, c => c.Name == "Ladder_L0" && c.TriangleCount == 1800);
        Assert.Contains(ladder.Children, c => c.Name == "Ladder_L1" && c.TriangleCount == 400);

        Assert.Contains(plan.Notes, n => n.Contains("Ladder") && n.Contains("only 2 of 3"));
        Assert.Contains(plan.Notes, n => n.Contains("Cab") && n.Contains("nothing to organize"));
    }
}
