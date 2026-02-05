using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SwiftXP.SPT.TheModfather.Server.Data;

public class ServerManifest(string[] includePatterns, string[] excludePatterns)
{
    private ConcurrentDictionary<string, ServerFileManifest> _files = new(StringComparer.OrdinalIgnoreCase);

    public void AddOrUpdateFile(ServerFileManifest serverFileManifest)
    {
        _files[serverFileManifest.RelativeFilePath] = serverFileManifest;
    }

    public bool ContainsFile(string relativeFilePath)
    {
        return _files.TryGetValue(relativeFilePath, out _);
    }

    public void RemoveFile(string relativeFilePath)
    {
        _files.TryRemove(relativeFilePath, out _);
    }

    public bool TryGetFile(string relativeFilePath, out ServerFileManifest? serverFileManifest)
    {
        return _files.TryGetValue(relativeFilePath, out serverFileManifest);
    }

    public int Count => _files.Count;

    public List<ServerFileManifest> Files
    {
        get
        {
            return [.. _files.Values];
        }
        set
        {
            _files = new ConcurrentDictionary<string, ServerFileManifest>(
                value.ToDictionary(x => x.RelativeFilePath, x => x, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase
            );
        }
    }

    public string[] IncludePatterns { get; set; } = includePatterns;

    public string[] ExcludePatterns { get; set; } = excludePatterns;
}