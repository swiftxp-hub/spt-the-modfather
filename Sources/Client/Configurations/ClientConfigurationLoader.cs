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

    private void MigrateIfNeeded(ClientConfiguration config)
    {
        if (!Version.TryParse(config.ConfigVersion, out Version? version))
        {
            version = new Version(0, 0, 0);
        }

        if (version < new Version(0, 3, 0))
        {
            simpleSptLogger.LogInfo("Migrating 'The Modfather' client configuration to version 0.3.0...");

            config.ExcludedPaths =
                [.. config.ExcludedPaths.Select(x => !LooksLikeAFile(x) && !x.Contains('*') && !x.Contains('?') ? $"{x.TrimEnd('/')}/**/*" : x)];

            config.HeadlessWhitelist =
                [.. config.HeadlessWhitelist.Select(x => !LooksLikeAFile(x) && !x.Contains('*') && !x.Contains('?') ? $"{x.TrimEnd('/')}/**/*" : x)];

            version = new Version("0.3.0");
            config.ConfigVersion = "0.3.0";
            Save(config);

            simpleSptLogger.LogInfo($"'The Modfather' client configuration migrated to version 0.3.0");
        }

        if (version < new Version(1, 0, 1))
        {
            simpleSptLogger.LogInfo("Migrating 'The Modfather' client configuration to version 1.0.1...");

            config.ExcludedPaths =
                [.. config.ExcludedPaths.Union([
                    "**/*.log",
                    "BepInEx/plugins/SAIN/BotTypes.json",
                    "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
                    "BepInEx/plugins/SAIN/Presets/**/*"
                ])];

            version = new Version("1.0.1");
            config.ConfigVersion = "1.0.1";
            Save(config);

            simpleSptLogger.LogInfo($"'The Modfather' client configuration migrated to version 1.0.1");
        }

        if (version < new Version(1, 1, 0))
        {
            simpleSptLogger.LogInfo("Migrating 'The Modfather'  client configuration to version 1.1.0...");

            if (config.ExcludedPaths.Contains("BepInEx/plugins/SAIN/BotTypes.json")
                && config.ExcludedPaths.Contains("BepInEx/plugins/SAIN/Default Bot Config Values/**/*")
                && config.ExcludedPaths.Contains("BepInEx/plugins/SAIN/Presets/**/*"))
            {
                config.ExcludedPaths =
                    [.. config.ExcludedPaths.Except([
                        "BepInEx/plugins/SAIN/BotTypes.json",
                        "BepInEx/plugins/SAIN/Default Bot Config Values/**/*",
                        "BepInEx/plugins/SAIN/Presets/**/*"
                    ])];

                config.ExcludedPaths =
                    [.. config.ExcludedPaths.Union([
                        "BepInEx/plugins/SAIN/**/*.json",
                    ])];
            }

            config.HeadlessWhitelist =
                [.. config.HeadlessWhitelist.Union([
                    "SwiftXP.SPT.TheModfather.Updater.exe",
                    "BepInEx/plugins/com.swiftxp.spt.themodfather/**/*",
                    "BepInEx/plugins/Fika/**/*",
                    "BepInEx/plugins/MergeConsumables/**/*",
                    "BepInEx/plugins/ozen-Foldables/**/*",
                    "BepInEx/plugins/s8_SPT_PatchCRC32/**/*",
                    "BepInEx/plugins/WTT-ClientCommonLib/**/*",
                    "BepInEx/plugins/NerfBotGrenades.dll",
                ])];

            version = new Version(AppMetadata.Version);
            config.ConfigVersion = AppMetadata.Version;
            Save(config);

            simpleSptLogger.LogInfo($"'The Modfather' client configuration migrated to version {AppMetadata.Version}");
        }
    }

    private static bool LooksLikeAFile(string path)
    {
        string[] validExtensions =
        [
            ".7z", ".a", ".apk", ".arj", ".asm", ".asp", ".aspx", ".assets", ".audiobakedata",
            ".avi", ".bak", ".bash", ".bat", ".bin", ".bmp", ".br", ".browser", ".bundle",
            ".bundles", ".bz2", ".bytes", ".c", ".cab", ".cat", ".cer", ".cfg", ".cgi",
            ".class", ".cmd", ".com", ".conf", ".config", ".cpp", ".cpl", ".crt", ".cs",
            ".css", ".csv", ".cur", ".dat", ".db", ".deb", ".delta", ".desktop", ".diff",
            ".dll", ".dmg", ".doc", ".docx", ".doorstop_version", ".drv", ".dump", ".env",
            ".err", ".exe", ".flv", ".gif", ".gz", ".h", ".hpp", ".htm", ".html", ".ico",
            ".img", ".inf", ".info", ".ini", ".iso", ".jar", ".java", ".jpeg", ".jpg",
            ".js", ".json", ".jsonc", ".key", ".ko", ".lib", ".linux", ".lnk", ".lock",
            ".log", ".lua", ".m3u", ".m4a", ".manifest", ".map", ".md", ".mdb", ".mkv",
            ".mov", ".mp3", ".mp4", ".mpeg", ".mpg", ".msc", ".msi", ".o", ".obj", ".odt",
            ".ogg", ".old", ".pages", ".patch", ".pcl", ".pdb", ".pdf", ".pem", ".pfx",
            ".php", ".pid", ".pkg", ".pl", ".pm", ".png", ".pot", ".ppt", ".pptx", ".prefab",
            ".ps1", ".pst", ".py", ".pyc", ".rar", ".rb", ".rc", ".reg", ".resource",
            ".ress", ".rpm", ".rst", ".rtf", ".run", ".sav", ".scr", ".service", ".sh",
            ".sharedassets", ".skin_mesh", ".so", ".spt-bak", ".sql", ".svg", ".swf",
            ".sys", ".tar", ".textures", ".tga", ".tgz", ".tiff", ".tmp", ".toml", ".ts",
            ".ttf", ".txt", ".vbs", ".wav", ".webm", ".webp", ".wma", ".wmv", ".woff",
            ".woff2", ".xls", ".xlsx", ".xml", ".xrageo", ".xramap", ".xz", ".yaml",
            ".yml", ".zip", ".zsh"
        ];

        return validExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}