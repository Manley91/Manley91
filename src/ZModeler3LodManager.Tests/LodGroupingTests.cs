using ZModeler3LodManager.Core.Models;
using ZModeler3LodManager.Core.Naming;
using ZModeler3LodManager.Core.Processing;
using Xunit;

namespace ZModeler3LodManager.Tests;

public class LodGroupingTests
{
    private static readonly LodNamingOptions DefaultOptions = new();

    [Fact]
    public void GroupDescendantsByBaseName_GroupsFlatSiblingsSharingABaseName()
    {
        var root = new SceneNode("Truck");
        root.AddChild(new SceneNode("Cab_high") { TriangleCount = 192 });
        root.AddChild(new SceneNode("Cab_low") { TriangleCount = 12 });
        root.AddChild(new SceneNode("Grille") { TriangleCount = 12 });

        var groups = LodGrouping.GroupDescendantsByBaseName(root, DefaultOptions)
            .ToDictionary(g => g.BaseName);

        Assert.Equal(2, groups["Cab"].Members.Count);
        Assert.Single(groups["Grille"].Members);
    }

    [Fact]
    public void GroupDescendantsByBaseName_GroupsVariantsNestedUnderAPart()
    {
        var root = new SceneNode("FireTruck");
        var body = new SceneNode("Body");
        body.AddChild(new SceneNode("Body_high") { TriangleCount = 5200 });
        body.AddChild(new SceneNode("Body_low") { TriangleCount = 900 });
        root.AddChild(body);
        root.AddChild(new SceneNode("Cab") { TriangleCount = 2100 });

        var groups = LodGrouping.GroupDescendantsByBaseName(root, DefaultOptions)
            .ToDictionary(g => g.BaseName);

        // "Body" the container itself is not a leaf, so it must not show up as a third member.
        Assert.Equal(2, groups["Body"].Members.Count);
        Assert.DoesNotContain(body, groups["Body"].Members);
        Assert.Single(groups["Cab"].Members);
    }

    [Fact]
    public void GroupDirectChildren_OnlyLooksOneLevelDeep()
    {
        var root = new SceneNode("FireTruck");
        var body = new SceneNode("Body");
        body.AddChild(new SceneNode("Body_high"));
        body.AddChild(new SceneNode("Body_low"));
        root.AddChild(body);
        root.AddChild(new SceneNode("Cab"));

        var groups = LodGrouping.GroupDirectChildren(root, DefaultOptions)
            .ToDictionary(g => g.BaseName);

        Assert.Single(groups["Body"].Members);
        Assert.Single(groups["Cab"].Members);
    }
}
