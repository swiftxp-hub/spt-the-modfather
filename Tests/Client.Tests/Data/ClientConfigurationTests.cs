using System;
using Xunit;
using SwiftXP.SPT.TheModfather.Client.Data;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Data;

public class ClientConfigurationTests
{
    [Fact]
    public void ConstructorSetsDefaultValues()
    {
        ClientConfiguration config = new();

        Assert.NotNull(config.ConfigVersion);
        Assert.Empty(config.ExcludePatterns);
    }

    [Fact]
    public void ExcludePatternsSetterHandlesNull()
    {
        ClientConfiguration config = new()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            ExcludePatterns = null
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        };

        Assert.NotNull(config.ExcludePatterns);
        Assert.Empty(config.ExcludePatterns);
    }

    [Fact]
    public void ExcludePatternsSetterNormalizesPaths()
    {
        ClientConfiguration config = new();
        string[] input =
        [
            "normal/path",
            "./relative/path",
            "/leading/slash",
            "trailing/slash/",
            "  whitespace  ",
            "mixed/./parts/"
        ];

        config.ExcludePatterns = input;

        Assert.Equal(6, config.ExcludePatterns.Length);
        Assert.Equal("normal/path", config.ExcludePatterns[0]);
        Assert.Equal("relative/path", config.ExcludePatterns[1]);
        Assert.Equal("leading/slash", config.ExcludePatterns[2]);
        Assert.Equal("trailing/slash", config.ExcludePatterns[3]);
        Assert.Equal("whitespace", config.ExcludePatterns[4]);
        Assert.Equal("mixed/./parts", config.ExcludePatterns[5]);
    }

    [Fact]
    public void ExcludePatternsSetterConvertsBackslashes()
    {
        ClientConfiguration config = new();
        string[] input =
        [
            @"windows\path",
            @".\relative\windows"
        ];

        config.ExcludePatterns = input;

        Assert.Equal("windows/path", config.ExcludePatterns[0]);
        Assert.Equal("relative/windows", config.ExcludePatterns[1]);
    }
}