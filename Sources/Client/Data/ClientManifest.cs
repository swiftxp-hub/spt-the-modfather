using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public class ClientManifest(DateTimeOffset lastSyncTimestamp, string serverUrl)
{
    private ConcurrentDictionary<string, ClientFileManifest> _files = [];

    public void AddOrUpdateFile(ClientFileManifest clientFileManifest)
    {
        _files[clientFileManifest.RelativeFilePath] = clientFileManifest;
    }

    public void RemoveFile(string relativeFilePath)
    {
        _files.TryRemove(relativeFilePath, out ClientFileManifest? _);
    }

    public int Count
    {
        get
        {
            return _files.Count;
        }
    }

    public List<ClientFileManifest> Files
    {
        get
        {
            return [.. _files.Values];
        }
        set
        {
            _files = new(value.ToDictionary(x => x.RelativeFilePath, x => x, StringComparer.OrdinalIgnoreCase));
        }
    }

    public DateTimeOffset LastSyncTimestamp { get; set; } = lastSyncTimestamp;

    public string ServerUrl { get; set; } = serverUrl;
}
