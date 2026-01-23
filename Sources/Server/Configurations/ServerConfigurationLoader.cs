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
    private static readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "user", "mods", "com.swiftxp.spt.themodfather", "config", "config.json");

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping 
    };

    public ServerConfiguration LoadOrCreate()
    {
        if (!File.Exists(_filePath))
        {
            var defaultConfig = new ServerConfiguration();
            Save(defaultConfig); 

            return defaultConfig;
        }

        try
        {
            string jsonString = File.ReadAllText(_filePath);
            var config = JsonSerializer.Deserialize<ServerConfiguration>(jsonString, _options);
            
            return config ?? new ServerConfiguration();
        }
        catch (JsonException)
        {
            logger.Error($"[ERROR] Configuration is invalid (syntax-error): {_filePath}");

            throw;
        }
    }

    public static void Save(ServerConfiguration config)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string jsonString = JsonSerializer.Serialize(config, _options);

        File.WriteAllText(_filePath, jsonString);
    }
}