using System;
using System.IO;
using Newtonsoft.Json;
using SwiftXP.SPT.Common.Loggers.Interfaces;
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
            ClientConfiguration defaultConfig = new ClientConfiguration();
            Save(defaultConfig);

            return defaultConfig;
        }

        try
        {
            string jsonString = File.ReadAllText(s_filePath);
            ClientConfiguration? config = JsonConvert.DeserializeObject<ClientConfiguration>(jsonString, s_options);

            return config ?? new ClientConfiguration();
        }
        catch (JsonException)
        {
            simpleSptLogger.LogError($"Configuration is invalid (syntax-error): {s_filePath}");

            throw;
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