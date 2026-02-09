using System;
using Xunit;
using SwiftXP.SPT.TheModfather.Server.Data;
using SwiftXP.SPT.Common.Extensions.FileSystem;

namespace SwiftXP.SPT.TheModfather.Server.Tests.Data;

public class ServerConfigurationTests
{
    [Fact]
    public void ConstructorSetsDefaultValues()
    {
        ServerConfiguration config = new();

        Assert.NotNull(config.ConfigVersion);
        Assert.NotEmpty(config.ConfigVersion);

        Assert.Contains("SwiftXP.SPT.TheModfather.Updater.exe", config.IncludePatterns);
        Assert.Contains("BepInEx/plugins/**/*", config.IncludePatterns);

        Assert.Contains("**/*.log", config.ExcludePatterns);

        Assert.Empty(config.FileHashBlacklist);
    }

    [Fact]
    public void IncludePatternsSetterNormalizesBackslashes()
    {
        ServerConfiguration config = new();
        string[] windowsPaths = ["BepInEx\\plugins\\mod.dll", "config\\settings.json"];

        config.IncludePatterns = windowsPaths;

        Assert.Equal("BepInEx/plugins/mod.dll", config.IncludePatterns[0]);
        Assert.Equal("config/settings.json", config.IncludePatterns[1]);
    }

    [Fact]
    public void ExcludePatternsSetterRemovesDotSlashPrefix()
    {
        ServerConfiguration config = new();
        string[] relativePaths = ["./logs/debug.log", "./user/cache/"];

        config.ExcludePatterns = relativePaths;

        Assert.Equal("logs/debug.log", config.ExcludePatterns[0]);
        Assert.Equal("user/cache", config.ExcludePatterns[1]);
    }

    [Fact]
    public void SetterTrimsWhitespaceAndSlashes()
    {
        ServerConfiguration config = new();
        string[] messyPaths = [" /folder/subfolder/ ", "  file.txt  "];

        config.IncludePatterns = messyPaths;

        Assert.Equal("folder/subfolder", config.IncludePatterns[0]);
        Assert.Equal("file.txt", config.IncludePatterns[1]);
    }

    [Fact]
    public void SetterHandlesNullReturnsEmptyArray()
    {
        ServerConfiguration config = new()
        {
            IncludePatterns = null!,
            ExcludePatterns = null!
        };

        Assert.NotNull(config.IncludePatterns);
        Assert.Empty(config.IncludePatterns);

        Assert.NotNull(config.ExcludePatterns);
        Assert.Empty(config.ExcludePatterns);
    }

    [Fact]
    public void SetterHandlesMixedScenarios()
    {
        ServerConfiguration config = new();

        string[] mixed = [
            ".\\BepInEx\\plugins\\",
            "/user/mods/ ",
        ];

        config.IncludePatterns = mixed;

        Assert.Equal("BepInEx/plugins", config.IncludePatterns[0]);
        Assert.Equal("user/mods", config.IncludePatterns[1]);
    }
}