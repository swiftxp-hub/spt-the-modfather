using System.Collections.Generic;
using System.Linq;
using SwiftXP.SPT.TheModfather.Server.Data;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Server.Tests.Data;

public class ServerManifestTests
{
    private static ServerFileManifest CreateFile(string path, string hash = "123")
    {
        return new ServerFileManifest(path, hash, 1024, DateTimeOffset.UtcNow);
    }

    [Fact]
    public void ConstructorSetsPatternsCorrectly()
    {
        string[] includes = ["**/*"];
        string[] excludes = ["*.log"];

        ServerManifest manifest = new(includes, excludes);

        Assert.Equal(includes, manifest.IncludePatterns);
        Assert.Equal(excludes, manifest.ExcludePatterns);
        Assert.Equal(0, manifest.Count);
        Assert.Empty(manifest.Files);
    }

    [Fact]
    public void AddOrUpdateFileAddsNewFile()
    {
        ServerManifest manifest = new([], []);
        ServerFileManifest file = CreateFile("BepInEx/plugins/mod.dll");

        manifest.AddOrUpdateFile(file);

        Assert.Equal(1, manifest.Count);
        Assert.True(manifest.ContainsFile("BepInEx/plugins/mod.dll"));
    }

    [Fact]
    public void AddOrUpdateFileUpdatesExistingFile()
    {
        ServerManifest manifest = new([], []);
        string path = "config.json";

        ServerFileManifest fileV1 = CreateFile(path, "hash_v1");
        ServerFileManifest fileV2 = CreateFile(path, "hash_v2");

        manifest.AddOrUpdateFile(fileV1);
        manifest.AddOrUpdateFile(fileV2);

        Assert.Equal(1, manifest.Count);

        bool found = manifest.TryGetFile(path, out ServerFileManifest? retrieved);
        Assert.True(found);
        Assert.Equal("hash_v2", retrieved!.Hash);
    }

    [Fact]
    public void DictionaryIsCaseInsensitiveAddAndContains()
    {
        ServerManifest manifest = new([], []);
        ServerFileManifest file = CreateFile("BepInEx/plugins/Mod.dll");

        manifest.AddOrUpdateFile(file);

        Assert.True(manifest.ContainsFile("bepinex/plugins/mod.dll"));
        Assert.True(manifest.ContainsFile("BEPINEX/PLUGINS/MOD.DLL"));
    }

    [Fact]
    public void TryGetFileReturnsFileCaseInsensitive()
    {
        ServerManifest manifest = new([], []);
        ServerFileManifest file = CreateFile("Data/Items.json");
        manifest.AddOrUpdateFile(file);

        bool result = manifest.TryGetFile("data/items.json", out ServerFileManifest? retrieved);

        Assert.True(result);
        Assert.NotNull(retrieved);
        Assert.Equal("Data/Items.json", retrieved!.RelativeFilePath);
    }

    [Fact]
    public void RemoveFileRemovesFileCaseInsensitive()
    {
        ServerManifest manifest = new([], []);
        manifest.AddOrUpdateFile(CreateFile("Log.txt"));

        manifest.RemoveFile("log.txt");

        Assert.Equal(0, manifest.Count);
        Assert.False(manifest.ContainsFile("Log.txt"));
    }

    [Fact]
    public void RemoveFileDoesNothingIfFileDoesNotExist()
    {
        ServerManifest manifest = new([], []);
        manifest.AddOrUpdateFile(CreateFile("KeepMe.txt"));

        manifest.RemoveFile("Ghost.txt");

        Assert.Equal(1, manifest.Count);
    }

    [Fact]
    public void FilesGetterReturnsSnapshotOfValues()
    {
        ServerManifest manifest = new([], []);
        manifest.AddOrUpdateFile(CreateFile("A"));
        manifest.AddOrUpdateFile(CreateFile("B"));

        List<ServerFileManifest> list = manifest.Files;

        Assert.Equal(2, list.Count);
        Assert.Contains(list, f => f.RelativeFilePath == "A");
        Assert.Contains(list, f => f.RelativeFilePath == "B");
    }

    [Fact]
    public void FilesSetterReplacesDictionaryAndMaintainsCaseInsensitivity()
    {
        ServerManifest manifest = new([], []);
        List<ServerFileManifest> newList =
        [
            CreateFile("NewFile.txt")
        ];

        manifest.Files = newList;

        Assert.Equal(1, manifest.Count);
        Assert.True(manifest.ContainsFile("newfile.txt"));
    }
}