namespace SwiftXP.SPT.TheModfather.Client;

public static class Constants
{
    public const string RoutePrefix = "/theModfather";

    public const string RouteGetServerManifest = "/getServerManifest";

    public const string RouteGetFileHashBlacklist = "/getFileHashBlacklist";

    public const string RouteGetFile = "/getFile";

    public const string BepInExDirectory = "BepInEx";

    public const string ModfatherDataDirectory = "TheModfather_Data";

    public const string StagingDirectory = "Staging";

    public const string ClientConfigurationFile = "clientConfiguration.json";

    public const string ClientManifestFile = "clientManifest.json";

    public const string FikaHeadlessModGuid = "com.fika.headless";

    public const string DeleteInstructionExtension = ".delete";

    public const string UpdaterExecutable = "SwiftXP.SPT.TheModfather.Updater.exe";

    public const int FileDownloadTimeoutInMinutes = 15;

    public const string ProcessIdParameter = "processid";

    public const string SilentParameter = "silent";
}