using ZModeler3LodManager.Core.Naming;
using Xunit;

namespace ZModeler3LodManager.Tests;

public class LodNamingTests
{
    private static readonly LodNamingOptions DefaultOptions = new();

    [Theory]
    [InlineData("Bumper_high", "Bumper")]
    [InlineData("Bumper_low", "Bumper")]
    [InlineData("Chassis.001", "Chassis")]
    [InlineData("Chassis.002", "Chassis")]
    [InlineData("Door (1)", "Door")]
    [InlineData("Door_L0", "Door")]
    [InlineData("Door_LOD2", "Door")]
    [InlineData("Grille", "Grille")]
    [InlineData("Cab_high_L0", "Cab")]
    public void GetBaseName_StripsKnownVariantMarkers(string input, string expected)
    {
        Assert.Equal(expected, LodNaming.GetBaseName(input, DefaultOptions));
    }

    [Fact]
    public void GetBaseName_NeverReturnsEmptyString()
    {
        var result = LodNaming.GetBaseName("_L0", DefaultOptions);
        Assert.Equal("_L0", result);
    }

    [Theory]
    [InlineData(0, "Bumper_L0")]
    [InlineData(1, "Bumper_L1")]
    [InlineData(2, "Bumper_L2")]
    public void BuildLodName_UsesConfiguredSuffixFormat(int level, string expected)
    {
        Assert.Equal(expected, LodNaming.BuildLodName("Bumper", DefaultOptions, level));
    }

    [Fact]
    public void BuildLodName_HonoursCustomSuffixFormat()
    {
        var options = new LodNamingOptions { SuffixFormat = "_LOD{n}" };
        Assert.Equal("Bumper_LOD3", LodNaming.BuildLodName("Bumper", options, 3));
    }
}
