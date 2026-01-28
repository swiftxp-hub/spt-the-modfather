using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.Common.Runtime;
using SwiftXP.SPT.TheModfather.Client.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Client.Configurations;

public class ClientConfigurationLoader(ISimpleSptLogger simpleSptLogger) : IClientConfigurationLoader
{
    private static readonly string s_filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Constants.ClientConfigurationPath));

    private static readonly JsonSerializerSettings s_options = new()
    {
        Formatting = Formatting.Indented,
    };

    public ClientConfiguration LoadOrCreate()
    {
        if (!File.Exists(s_filePath))
        {
            ClientConfiguration defaultConfig = new();
            Save(defaultConfig);

            return defaultConfig;
        }

        try
        {
            string jsonString = File.ReadAllText(s_filePath);
            ClientConfiguration? config = JsonConvert.DeserializeObject<ClientConfiguration>(jsonString, s_options);

            ClientConfiguration loadedConfig = config ?? new ClientConfiguration();
            MigrateIfNeeded(loadedConfig);

            return loadedConfig;
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"Configuration is invalid (syntax-error): {s_filePath}");

            throw;
        }
    }

    private void MigrateIfNeeded(ClientConfiguration config)
    {
        if (!Version.TryParse(config.ConfigVersion, out Version? version))
        {
            version = new Version(0, 0, 0);
        }

        if (version < new Version(0, 3, 0))
        {
            simpleSptLogger.LogInfo("Migrating client configuration to latest version...");

            config.ExcludedPaths =
                [.. config.ExcludedPaths.Select(x => !Path.HasExtension(x) && !x.Contains('*') && !x.Contains('?') ? $"{x.TrimEnd('/')}/**/*" : x)];

            config.HeadlessWhitelist =
                [.. config.HeadlessWhitelist.Select(x => !Path.HasExtension(x) && !x.Contains('*') && !x.Contains('?') ? $"{x.TrimEnd('/')}/**/*" : x)];

            config.ConfigVersion = AppMetadata.Version;
            Save(config);

            simpleSptLogger.LogInfo($"Client configuration migrated to version {AppMetadata.Version}");
        }
    }

    public static void Save(ClientConfiguration config)
    {
        string? directory = Path.GetDirectoryName(s_filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string jsonString = JsonConvert.SerializeObject(config, s_options);

        File.WriteAllText(s_filePath, jsonString);
    }
}