using System;
using System.Collections.Generic;
using SwiftXP.SPT.TheModfather.Client.Data;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Client.Tests.Data;

public class ClientManifestTests
{
    [Fact]
    public void ConstructorSetsPropertiesCorrectly()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        string url = "http://localhost:8080";

        ClientManifest manifest = new(timestamp, url);

        Assert.Equal(timestamp, manifest.LastSyncTimestamp);
        Assert.Equal(url, manifest.ServerUrl);
        Assert.Equal(0, manifest.Count);
        Assert.Empty(manifest.Files);
    }

    [Fact]
    public void AddOrUpdateFileAddsNewFile()
    {
        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://test");
        ClientFileManifest file = new("test/file.txt", string.Empty, 0, DateTimeOffset.UtcNow);

        manifest.AddOrUpdateFile(file);

        Assert.Equal(1, manifest.Count);
        Assert.Single(manifest.Files);
        Assert.Equal("test/file.txt", manifest.Files[0].RelativeFilePath);
    }

    [Fact]
    public void AddOrUpdateFileUpdatesExistingFile()
    {
        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://test");
        string path = "config.json";

        ClientFileManifest file1 = new(path, string.Empty, 0, DateTimeOffset.UtcNow);
        ClientFileManifest file2 = new(path, string.Empty, 0, DateTimeOffset.UtcNow);

        manifest.AddOrUpdateFile(file1);
        manifest.AddOrUpdateFile(file2);

        Assert.Equal(1, manifest.Count);
        Assert.Contains(manifest.Files, f => f == file2);
    }

    [Fact]
    public void RemoveFileRemovesExistingFile()
    {
        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://test");

        string path = "remove/me.dll";
        ClientFileManifest file = new(path, string.Empty, 0, DateTimeOffset.UtcNow);

        manifest.AddOrUpdateFile(file);
        manifest.RemoveFile(path);

        Assert.Equal(0, manifest.Count);
        Assert.Empty(manifest.Files);
    }

    [Fact]
    public void RemoveFileDoesNothingIfFileDoesNotExist()
    {
        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://test");
        ClientFileManifest file = new("keep.dll", string.Empty, 0, DateTimeOffset.UtcNow);

        manifest.AddOrUpdateFile(file);
        manifest.RemoveFile("ghost.dll");

        Assert.Equal(1, manifest.Count);
    }

    [Fact]
    public void FilesSetterReplacesCollection()
    {
        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://test");
        List<ClientFileManifest> newFiles =
        [
            new("a.txt", string.Empty, 0, DateTimeOffset.UtcNow),
            new("b.txt", string.Empty, 0, DateTimeOffset.UtcNow)
        ];

        manifest.Files = newFiles;

        Assert.Equal(2, manifest.Count);
        Assert.Contains(manifest.Files, f => f.RelativeFilePath == "a.txt");
        Assert.Contains(manifest.Files, f => f.RelativeFilePath == "b.txt");
    }

    [Fact]
    public void FilesSetterHandlesCaseInsensitiveKeysInInput()
    {
        ClientManifest manifest = new(DateTimeOffset.UtcNow, "http://test");
        List<ClientFileManifest> files =
        [
            new("File.txt", string.Empty, 0, DateTimeOffset.UtcNow)
        ];

        manifest.Files = files;

        bool containsLower = manifest.Files.Exists(f => f.RelativeFilePath == "File.txt");
        Assert.True(containsLower);
    }
}