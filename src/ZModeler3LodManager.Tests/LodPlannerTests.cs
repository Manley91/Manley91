using ZModeler3LodManager.Core.Models;
using ZModeler3LodManager.Core.Naming;
using ZModeler3LodManager.Core.Processing;
using Xunit;

namespace ZModeler3LodManager.Tests;

public class LodPlannerTests
{
    private static SceneNode BuildPoliceCarWithoutLods()
    {
        var root = new SceneNode("PoliceCar");
        root.AddChild(new SceneNode("Body") { TriangleCount = 4200 });
        root.AddChild(new SceneNode("Interior") { TriangleCount = 3100 });
        root.AddChild(new SceneNode("Wheels") { TriangleCount = 2600 });
        root.AddChild(new SceneNode("Lights") { TriangleCount = 400 });
        root.AddChild(new SceneNode("Grille") { TriangleCount = 350 });
        root.AddChild(new SceneNode("Bumper") { TriangleCount = 900 });
        return root;
    }

    [Fact]
    public void PlanCreateCopies_DuplicatesEveryPartIntoConfiguredLevelCount()
    {
        var root = BuildPoliceCarWithoutLods();
        var options = new LodNamingOptions { LevelCount = 3 };

        var plan = LodPlanner.PlanCreateCopies(new[] { root }, options);

        // 6 parts, each renamed to _L0 plus 2 duplicates (_L1, _L2).
        Assert.Equal(6, plan.Renames.Count);
        Assert.All(plan.Renames, r => Assert.EndsWith("_L0", r.NewName));
        Assert.Equal(12, plan.Duplications.Count);
        Assert.Empty(plan.Notes);

        LodPlanApplier.Apply(plan);

        Assert.Equal(18, root.Children.Count);
        var bodyLevels = root.Children.Where(c => c.Name.StartsWith("Body_L")).ToList();
        Assert.Equal(3, bodyLevels.Count);
        Assert.All(bodyLevels, c => Assert.Equal(4200, c.TriangleCount));
    }

    [Fact]
    public void PlanCreateCopies_SkipsPartsThatAlreadyHaveLodVariants()
    {
        var root = new SceneNode("Truck");
        root.AddChild(new SceneNode("Cab_high") { TriangleCount = 192 });
        root.AddChild(new SceneNode("Cab_low") { TriangleCount = 12 });
        root.AddChild(new SceneNode("Grille") { TriangleCount = 12 });
        var options = new LodNamingOptions { LevelCount = 3 };

        var plan = LodPlanner.PlanCreateCopies(new[] { root }, options);

        // Only "Grille" (no existing variants) should be processed.
        Assert.Single(plan.Renames);
        Assert.Equal("Grille_L0", plan.Renames[0].NewName);
        Assert.Equal(2, plan.Duplications.Count);
        Assert.Contains(plan.Notes, n => n.Contains("Cab_high") && n.Contains("already has LOD variants"));
    }

    [Fact]
    public void PlanOrganizeExisting_OrdersVariantsByTriangleCountDescending()
    {
        var root = new SceneNode("Truck");
        root.AddChild(new SceneNode("Cab_high") { TriangleCount = 192 });
        root.AddChild(new SceneNode("Cab_low") { TriangleCount = 12 });
        root.AddChild(new SceneNode("Grille") { TriangleCount = 12 });
        var options = new LodNamingOptions { LevelCount = 3 };

        var plan = LodPlanner.PlanOrganizeExisting(new[] { root }, options);

        var renamesByOldName = plan.Renames.ToDictionary(r => r.OldName);
        Assert.Equal("Cab_L0", renamesByOldName["Cab_high"].NewName);
        Assert.Equal("Cab_L1", renamesByOldName["Cab_low"].NewName);
        Assert.DoesNotContain(plan.Renames, r => r.OldName == "Grille");
        Assert.Contains(plan.Notes, n => n.Contains("Grille") && n.Contains("nothing to organize"));
        Assert.Contains(plan.Notes, n => n.Contains("Cab") && n.Contains("only 2 of 3"));
    }

    [Fact]
    public void PlanOrganizeExisting_CapsRenamesAtConfiguredLevelCountAndNotesTheOverflow()
    {
        var root = new SceneNode("Truck");
        root.AddChild(new SceneNode("Door_a") { TriangleCount = 400 });
        root.AddChild(new SceneNode("Door_b") { TriangleCount = 300 });
        root.AddChild(new SceneNode("Door_c") { TriangleCount = 200 });
        root.AddChild(new SceneNode("Door_d") { TriangleCount = 100 });
        var options = new LodNamingOptions { LevelCount = 2, VariantMarkerPatterns = { "_[a-z]$" } };

        var plan = LodPlanner.PlanOrganizeExisting(new[] { root }, options);

        Assert.Equal(2, plan.Renames.Count);
        Assert.Contains(plan.Notes, n => n.Contains("2 lowest-detail variant"));
    }

    [Fact]
    public void Apply_IsIdempotentWhenNothingNeedsRenaming()
    {
        var root = new SceneNode("Truck");
        var already = new SceneNode("Bumper_L0") { TriangleCount = 100 };
        root.AddChild(already);

        var plan = LodPlanner.PlanOrganizeExisting(new[] { root }, new LodNamingOptions());

        Assert.Empty(plan.Renames);
    }
}
