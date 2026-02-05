using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.Common.Environment;
using SwiftXP.SPT.Common.Json;
using SwiftXP.SPT.TheModfather.Server.Data;

namespace SwiftXP.SPT.TheModfather.Server.Repositories;

[Injectable(InjectionType.Singleton)]
public class ServerConfigurationRepository(ISptLogger<ServerConfigurationRepository> sptLogger,
    IBaseDirectoryLocator baseDirectoryLocator,
    IJsonFileSerializer jsonFileSerializer) : IServerConfigurationRepository, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private volatile ServerConfiguration? _cachedConfig;
    private FileSystemWatcher? _watcher;
    private int _fileSystemWatcherCounts;

    public event EventHandler<ServerConfiguration>? OnConfigurationChanged;

    public async Task<ServerConfiguration> LoadOrCreateDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_cachedConfig != null)
                return _cachedConfig;

            string baseDirectory = baseDirectoryLocator.GetBaseDirectory();
            string configFilePath = Path.GetFullPath(Path.Combine(baseDirectory, Constants.ServerConfigurationDirectory, Constants.ServerConfigurationFile));

            if (!File.Exists(configFilePath))
            {
                sptLogger.Info($"{Constants.LoggerPrefix}Server-Configuration missing. Creating default configuration...");

                _cachedConfig = new ServerConfiguration();
                await Save(_cachedConfig, configFilePath, cancellationToken);

                return _cachedConfig;
            }

            try
            {
                _cachedConfig = await jsonFileSerializer.DeserializeJsonFileAsync<ServerConfiguration>(configFilePath, cancellationToken);
            }
            catch (JsonException)
            {
                sptLogger.Error($"{Constants.LoggerPrefix}[ERROR] Configuration is invalid (syntax-error): {configFilePath}");
                throw;
            }

            if (_cachedConfig == null)
            {
                sptLogger.Warning($"{Constants.LoggerPrefix}[ERROR] Configuration was empty. Restoring default...");
                _cachedConfig = new ServerConfiguration();
                await Save(_cachedConfig, configFilePath, cancellationToken);
            }

            return _cachedConfig;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void WatchForChanges()
    {
        if (_watcher != null)
            return;

        string baseDirectory = baseDirectoryLocator.GetBaseDirectory();
        string serverConfigurationDirectory = Path.GetFullPath(Path.Combine(baseDirectory, Constants.ServerConfigurationDirectory));

        if (!Directory.Exists(serverConfigurationDirectory))
        {
            Directory.CreateDirectory(serverConfigurationDirectory);
        }

        _watcher = new FileSystemWatcher(serverConfigurationDirectory)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            Filter = Constants.ServerConfigurationFile,
            EnableRaisingEvents = true
        };

        _watcher.Created += async (_, e) => await HandleFileChangeAsync(e.FullPath);
        _watcher.Changed += async (_, e) => await HandleFileChangeAsync(e.FullPath);
        _watcher.Deleted += async (_, _) => await HandleFileResetAsync();
        _watcher.Renamed += async (_, _) => await HandleFileResetAsync();
    }

    private async Task HandleFileChangeAsync(string fullPath)
    {
        Interlocked.Increment(ref _fileSystemWatcherCounts);
        await Task.Delay(150);

        if (Interlocked.Decrement(ref _fileSystemWatcherCounts) == 0)
        {
            sptLogger.Info($"{Constants.LoggerPrefix}Server-Configuration file change detected. Updating cache...");

            const int maxRetries = 5;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await _lock.WaitAsync();

                    ServerConfiguration? newConfig = await jsonFileSerializer.DeserializeJsonFileAsync<ServerConfiguration>(fullPath);
                    if (newConfig != null)
                    {
                        _cachedConfig = newConfig;
                        sptLogger.Info($"{Constants.LoggerPrefix}Server-Configuration cache successfully updated.");

                        NotifyConfigurationChanged(newConfig);
                    }

                    break;
                }
                catch (IOException) when (i < maxRetries - 1)
                {
                    await Task.Delay(500);
                }
                catch (Exception ex)
                {
                    sptLogger.Error($"{Constants.LoggerPrefix}[ERROR] Reloading configuration failed: {ex.Message}");
                    break;
                }
                finally
                {
                    _lock.Release();
                }
            }
        }
    }

    private async Task HandleFileResetAsync()
    {
        sptLogger.Warning($"{Constants.LoggerPrefix}[Warning] Server-Configuration deleted or renamed. Resetting cache and restoring file...");

        _cachedConfig = null;
        await LoadOrCreateDefaultAsync();
    }

    private void NotifyConfigurationChanged(ServerConfiguration config)
    {
        OnConfigurationChanged?.Invoke(this, config);
    }

    private async Task Save(ServerConfiguration config, string configFilePath, CancellationToken cancellationToken = default)
    {
        await jsonFileSerializer.SerializeJsonFileAsync(configFilePath, config, cancellationToken);
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _lock.Dispose();

        GC.SuppressFinalize(this);
    }
}