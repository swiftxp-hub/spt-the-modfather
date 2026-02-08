using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.FileSystemGlobbing;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.Extensions.FileSystem;
using SwiftXP.SPT.Common.IO.Hashing;
using SwiftXP.SPT.TheModfather.Server.Data;
using SwiftXP.SPT.TheModfather.Server.Repositories;

namespace SwiftXP.SPT.TheModfather.Server.Services;

[Injectable(InjectionType.Singleton)]
public class ServerManifestManager : IServerManifestManager, IDisposable
{
    private const int MaxHashingRetries = 5;

    private readonly ISptLogger<ServerManifestManager> _sptLogger;
    private readonly IBaseDirectoryLocator _baseDirectoryLocator;
    private readonly IServerConfigurationRepository _serverConfigurationRepository;
    private readonly IXxHash128FileHasher _xxHash128FileHasher;

    private ServerManifest? _cachedServerManifest;
    private FileSystemWatcher? _fileSystemWatcher;
    private Matcher? _matcher;

    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _globalCancellationTokenSource = new();

    private readonly Channel<string> _eventChannel;

    private bool _isDisposed;

    public ServerManifestManager(
        ISptLogger<ServerManifestManager> sptLogger,
        IBaseDirectoryLocator baseDirectoryLocator,
        IServerConfigurationRepository serverConfigurationRepository,
        IXxHash128FileHasher xxHash128FileHasher)
    {
        _sptLogger = sptLogger;
        _baseDirectoryLocator = baseDirectoryLocator;
        _serverConfigurationRepository = serverConfigurationRepository;
        _xxHash128FileHasher = xxHash128FileHasher;

        _serverConfigurationRepository.OnConfigurationChanged += HandleConfigurationChanged;

        _eventChannel = Channel.CreateUnbounded<string>();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;

        _serverConfigurationRepository.OnConfigurationChanged -= HandleConfigurationChanged;

        if (_fileSystemWatcher != null)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Error -= OnWatcherError;
            _fileSystemWatcher.Dispose();
        }

        _globalCancellationTokenSource.Cancel();

        _eventChannel.Writer.TryComplete();

        _globalCancellationTokenSource.Dispose();
        _semaphore.Dispose();

        GC.SuppressFinalize(this);
    }

    public async Task<ServerManifest> GetServerManifestAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedServerManifest == null)
            await UpdateManifestInternalAsync(null, cancellationToken);

        return _cachedServerManifest!;
    }

    public void WatchForChanges()
    {
        if (_fileSystemWatcher != null || _isDisposed)
            return;

        _ = Task.Run(ProcessEventsLoop);

        string baseDirectory = _baseDirectoryLocator.GetBaseDirectory();

        _fileSystemWatcher = new FileSystemWatcher(baseDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite
                         | NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.Size
                         | NotifyFilters.CreationTime,
            Filter = "*",
            IncludeSubdirectories = true,
            EnableRaisingEvents = true,
            InternalBufferSize = 65536
        };

        _fileSystemWatcher.Created += (s, e) => _ = HandleFileChangeAsync(e.FullPath);
        _fileSystemWatcher.Changed += (s, e) => _ = HandleFileChangeAsync(e.FullPath);
        _fileSystemWatcher.Renamed += (s, e) => _ = HandleFileRenamedAsync(e.OldFullPath, e.FullPath);
        _fileSystemWatcher.Deleted += (s, e) => HandleFileDeleted(e.FullPath);

        _fileSystemWatcher.Error += OnWatcherError;
    }

    private async Task ProcessEventsLoop()
    {
        HashSet<string> batch = [];
        CancellationToken token = _globalCancellationTokenSource.Token;

        try
        {
            while (await _eventChannel.Reader.WaitToReadAsync(token))
            {
                while (_eventChannel.Reader.TryRead(out string? path))
                    batch.Add(path);

                if (batch.Count == 0)
                    continue;

                await Task.Delay(500, token);

                while (_eventChannel.Reader.TryRead(out string? path))
                    batch.Add(path);

                foreach (string fullPath in batch)
                {
                    if (_isDisposed)
                        break;

                    await _semaphore.WaitAsync(token);

                    try
                    {
                        await ProcessSingleFileChangeAsync(fullPath, token);
                    }
                    catch (Exception ex)
                    {
                        _sptLogger.Error($"{Constants.LoggerPrefix}Error processing batched file {fullPath}: {ex.Message}");
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                }

                batch.Clear();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _sptLogger.Error($"{Constants.LoggerPrefix}Critical error in ProcessEventsLoop: {ex}");
        }
    }

    private void HandleConfigurationChanged(object? sender, ServerConfiguration e)
    {
        _ = UpdateManifestInternalAsync(e);
    }

    private async Task UpdateManifestInternalAsync(ServerConfiguration? newConfig = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        using CancellationTokenSource linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationTokenSource.Token, cancellationToken);
        CancellationToken linkedCancellationToken = linkedCancellationTokenSource.Token;

        try
        {
            await _semaphore.WaitAsync(linkedCancellationToken);

            try
            {
                if (newConfig == null && _cachedServerManifest != null && _matcher != null)
                    return;

                ServerConfiguration config = newConfig ?? await _serverConfigurationRepository.LoadOrCreateDefaultAsync(linkedCancellationToken);

                _sptLogger.Info($"{Constants.LoggerPrefix}Rebuilding Server-Manifest (Patterns: {config.IncludePatterns.Length} inc / {config.ExcludePatterns.Length} exc)...");

                Matcher newMatcher = new();
                newMatcher.AddIncludePatterns(config.IncludePatterns);
                newMatcher.AddExcludePatterns(config.ExcludePatterns);
                _matcher = newMatcher;

                ServerManifest newManifest = new(config.IncludePatterns, config.ExcludePatterns);

                Stopwatch stopWatch = Stopwatch.StartNew();
                string baseDirectory = _baseDirectoryLocator.GetBaseDirectory();

                Dictionary<string, FileInfo> filesFound = baseDirectory.FindFilesByPattern(config.IncludePatterns, config.ExcludePatterns)
                    .ToDictionary(x => x.FullName);

                Dictionary<string, string> hashes = await _xxHash128FileHasher.GetFileHashesAsync(filesFound.Values, linkedCancellationToken);

                foreach (KeyValuePair<string, string> kvp in hashes)
                {
                    linkedCancellationToken.ThrowIfCancellationRequested();

                    if (filesFound.TryGetValue(kvp.Key, out FileInfo? fileInfo))
                    {
                        string webPath = Path.GetRelativePath(baseDirectory, kvp.Key).GetWebFriendlyPath();
                        newManifest.AddOrUpdateFile(new ServerFileManifest(webPath, kvp.Value, fileInfo.Length, fileInfo.LastWriteTimeUtc));
                    }
                }

                _cachedServerManifest = newManifest;

                stopWatch.Stop();
                _sptLogger.Info($"{Constants.LoggerPrefix}Server-Manifest rebuild completed: {hashes.Count} files in {stopWatch.ElapsedMilliseconds}ms.");
            }
            finally
            {
                if (!_isDisposed)
                    _semaphore.Release();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _sptLogger.Error($"{Constants.LoggerPrefix}Error during manifest update: {ex.Message}");
        }
    }

    private async Task HandleFileChangeAsync(string fullPath)
    {
        try
        {
            if (_isDisposed) return;
            await _eventChannel.Writer.WriteAsync(fullPath);
        }
        catch (Exception ex)
        {
            _sptLogger.Error($"{Constants.LoggerPrefix}Error queuing file change for {fullPath}: {ex.Message}");
        }
    }

    private async Task HandleFileRenamedAsync(string oldPath, string newPath)
    {
        HandleFileDeleted(oldPath);

        if (Directory.Exists(newPath))
            return;

        await EnsureInitializedAsync(_globalCancellationTokenSource.Token);

        string baseDirectory = _baseDirectoryLocator.GetBaseDirectory();
        string newRelative = Path.GetRelativePath(baseDirectory, newPath).GetWebFriendlyPath();

        if (IsFileRelevant(newRelative))
            await HandleFileChangeAsync(newPath);
    }

    private void HandleFileDeleted(string fullPath)
    {
        try
        {
            if (_cachedServerManifest == null)
                return;

            string baseDirectory = _baseDirectoryLocator.GetBaseDirectory();
            string relativePath = Path.GetRelativePath(baseDirectory, fullPath).GetWebFriendlyPath();

            if (_cachedServerManifest.ContainsFile(relativePath))
            {
                _cachedServerManifest.RemoveFile(relativePath);
                _sptLogger.Info($"{Constants.LoggerPrefix}File removed: {relativePath}");
            }
        }
        catch (Exception ex)
        {
            _sptLogger.Error($"{Constants.LoggerPrefix}Error handling file deletion for {fullPath}: {ex.Message}");
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _sptLogger.Warning($"{Constants.LoggerPrefix}FileSystemWatcher buffer overflow or error. Triggering full manifest rebuild.");
        _ = UpdateManifestInternalAsync();
    }

    private async Task ProcessSingleFileChangeAsync(string fullPath, CancellationToken token)
    {
        if (Directory.Exists(fullPath))
            return;

        await EnsureInitializedAsync(token);

        string baseDirectory = _baseDirectoryLocator.GetBaseDirectory();
        string relativePath = Path.GetRelativePath(baseDirectory, fullPath).GetWebFriendlyPath();

        if (!IsFileRelevant(relativePath))
            return;

        await UpdateFileHashWithRetryAsync(fullPath, relativePath, token);
    }

    private async Task EnsureInitializedAsync(CancellationToken token)
    {
        if (_matcher == null || _cachedServerManifest == null)
            await UpdateManifestInternalAsync(null, token);
    }

    private bool IsFileRelevant(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath))
            return false;

        string fileName = Path.GetFileName(relativePath);

        if (fileName.StartsWith("~$", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return _matcher?.Match(relativePath).HasMatches ?? false;
    }

    private async Task UpdateFileHashWithRetryAsync(string fullPath, string relativePath, CancellationToken cancellationToken)
    {
        for (int i = 0; i < MaxHashingRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                FileInfo fileInfo = new(fullPath);
                if (!fileInfo.Exists)
                    return;

                if (fileInfo.Length == 0)
                {
                    await Task.Delay(200, cancellationToken);
                    fileInfo.Refresh();

                    if (fileInfo.Length == 0 && i < MaxHashingRetries - 1)
                        continue;
                }

                if (_cachedServerManifest != null && _cachedServerManifest.TryGetFile(relativePath, out ServerFileManifest? existing))
                {
                    if (existing!.SizeInBytes == fileInfo.Length && existing.InstalledAt == fileInfo.LastWriteTimeUtc)
                        return;
                }

                string? hash = await _xxHash128FileHasher.GetFileHashAsync(fileInfo, cancellationToken);

                if (hash != null && _cachedServerManifest != null)
                {
                    _cachedServerManifest.AddOrUpdateFile(new ServerFileManifest(relativePath, hash, fileInfo.Length, fileInfo.LastWriteTimeUtc));
                    _sptLogger.Info($"{Constants.LoggerPrefix}File updated: {relativePath}");
                }

                return;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                if (i < MaxHashingRetries - 1)
                {
                    int delay = 50 * (int)Math.Pow(2, i);
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _sptLogger.Warning($"{Constants.LoggerPrefix}Timeout waiting for file unlock: {relativePath} - {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _sptLogger.Error($"{Constants.LoggerPrefix}Unexpected error hashing {relativePath}: {ex}");
                return;
            }
        }
    }
}