using System;
using System.IO;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;
using SwiftXP.SPT.TheModfather.Server.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Server.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Server.Configurations;

[Injectable(InjectionType.Scoped)]
public class ServerConfigurationLoader(ISptLogger<ServerConfigurationLoader> logger) : IServerConfigurationLoader
{
    private static readonly string s_filePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, Constants.ServerConfigurationPath));

    private static readonly JsonSerializerOptions s_options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public ServerConfiguration LoadOrCreate()
    {
        if (!File.Exists(s_filePath))
        {
            logger.Info("Server-Configuration for 'The Modfather' missing. Creating default configuration...");

            ServerConfiguration defaultConfig = new();
            Save(defaultConfig);

            return defaultConfig;
        }

        try
        {
            string jsonString = File.ReadAllText(s_filePath);
            ServerConfiguration? config = JsonSerializer.Deserialize<ServerConfiguration>(jsonString, s_options);

            return config ?? new ServerConfiguration();
        }
        catch (JsonException)
        {
            logger.Error($"[ERROR] Configuration is invalid (syntax-error): {s_filePath}");

            throw;
        }
    }

    public static void Save(ServerConfiguration config)
    {
        string? directory = Path.GetDirectoryName(s_filePath);

        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string jsonString = JsonSerializer.Serialize(config, s_options);

        File.WriteAllText(s_filePath, jsonString);
    }
}