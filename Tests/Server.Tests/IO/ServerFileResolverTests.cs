using System;
using System.IO;
using Moq;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.TheModfather.Server.IO;
using Xunit;

namespace SwiftXP.SPT.TheModfather.Server.Tests.IO;

public class ServerFileResolverTests
{
    private sealed class TempDirectory : IDisposable
    {
        public DirectoryInfo DirInfo { get; }

        public TempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DirInfo = Directory.CreateDirectory(path);
        }

        public void CreateFile(string relativePath)
        {
            string fullPath = Path.Combine(DirInfo.FullName, relativePath);
            string? dir = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, "dummy content");
        }

        public void Dispose()
        {
            if (DirInfo.Exists)
            {
                try { DirInfo.Delete(true); } catch { }
            }
        }
    }

    private readonly Mock<IBaseDirectoryLocator> _baseDirMock;

    public ServerFileResolverTests()
    {
        _baseDirMock = new Mock<IBaseDirectoryLocator>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetFileInfoReturnsNullForInvalidPaths(string? invalidPath)
    {
        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo(invalidPath!, ["**/*"], []);

        Assert.Null(result);
    }

    [Fact]
    public void GetFileInfoReturnsNullWhenFileDoesNotExist()
    {
        using TempDirectory temp = new();
        _baseDirMock.Setup(x => x.GetBaseDirectory()).Returns(temp.DirInfo.FullName);

        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo("ghost.txt", ["**/*"], []);

        Assert.Null(result);
    }

    [Fact]
    public void GetFileInfoReturnsFileInfoWhenFileExistsAndMatchesInclude()
    {
        using TempDirectory temp = new();
        _baseDirMock.Setup(x => x.GetBaseDirectory()).Returns(temp.DirInfo.FullName);

        temp.CreateFile("valid.txt");

        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo("valid.txt", ["*.txt"], []);

        Assert.NotNull(result);
        Assert.True(result!.Exists);
        Assert.Equal("valid.txt", result.Name);
    }

    [Fact]
    public void GetFileInfoReturnsNullWhenFileExcluded()
    {
        using TempDirectory temp = new();
        _baseDirMock.Setup(x => x.GetBaseDirectory()).Returns(temp.DirInfo.FullName);

        temp.CreateFile("secret.log");

        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo("secret.log", ["**/*"], ["*.log"]);

        Assert.Null(result);
    }

    [Fact]
    public void GetFileInfoReturnsNullWhenNotIncluded()
    {
        using TempDirectory temp = new();

        _baseDirMock.Setup(x => x.GetBaseDirectory())
            .Returns(temp.DirInfo.FullName);

        temp.CreateFile("data.json");

        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo("data.json", ["*.txt"], []);

        Assert.Null(result);
    }

    [Fact]
    public void GetFileInfoPreventsPathTraversal()
    {
        using TempDirectory temp = new();
        _baseDirMock.Setup(x => x.GetBaseDirectory()).Returns(temp.DirInfo.FullName);

        string outsideFile = Path.Combine(Path.GetTempPath(), "hack.txt");
        File.WriteAllText(outsideFile, "secret");

        try
        {
            ServerFileResolver resolver = new(_baseDirMock.Object);

            string maliciousPath = Path.Combine("..", "hack.txt");
            FileInfo? result = resolver.GetFileInfo(maliciousPath, ["**/*"], []);

            Assert.Null(result);
        }
        finally
        {
            if (File.Exists(outsideFile))
                File.Delete(outsideFile);
        }
    }

    [Fact]
    public void GetFileInfoHandlesNestedFoldersCorrectly()
    {
        using TempDirectory temp = new();
        _baseDirMock.Setup(x => x.GetBaseDirectory()).Returns(temp.DirInfo.FullName);

        temp.CreateFile("BepInEx/plugins/mod.dll");

        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo("BepInEx/plugins/mod.dll", ["BepInEx/**/*"], []);

        Assert.NotNull(result);
        Assert.Equal("mod.dll", result.Name);
    }

    [Fact]
    public void GetFileInfoReturnsNullOnException()
    {
        _baseDirMock.Setup(x => x.GetBaseDirectory()).Throws(new UnauthorizedAccessException("Access denied"));

        ServerFileResolver resolver = new(_baseDirMock.Object);

        FileInfo? result = resolver.GetFileInfo("test.txt", ["*"], []);

        Assert.Null(result);
    }
}