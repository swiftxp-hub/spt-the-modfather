using System;
using System.IO;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Loggers.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Configurations.Interfaces;
using SwiftXP.SPT.TheModfather.Client.Configurations.Models;

namespace SwiftXP.SPT.TheModfather.Client.Configurations;

public class ClientConfigurationLoader(ISimpleSptLogger simpleSptLogger) : IClientConfigurationLoader
{
    private static readonly string _filePath = Path.Combine(AppContext.BaseDirectory, "TheModfather_Data", "config.json");

    private static readonly JsonSerializerSettings _options = new()
    {
        Formatting = Formatting.Indented,
    };

    public ClientConfiguration LoadOrCreate()
    {
        if (!File.Exists(_filePath))
        {
            var defaultConfig = new ClientConfiguration();
            Save(defaultConfig); 

            return defaultConfig;
        }

        try
        {
            string jsonString = File.ReadAllText(_filePath);
            ClientConfiguration? config = JsonConvert.DeserializeObject<ClientConfiguration>(jsonString, _options);
            
            return config ?? new ClientConfiguration();
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"[ERROR] Configuration is invalid (syntax-error): {_filePath}");

            throw;
        }
    }

    public static void Save(ClientConfiguration config)
    {
        string? directory = Path.GetDirectoryName(_filePath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string jsonString = JsonConvert.SerializeObject(config, _options);

        File.WriteAllText(_filePath, jsonString);
    }
}