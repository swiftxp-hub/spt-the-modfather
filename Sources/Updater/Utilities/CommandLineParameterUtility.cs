using Microsoft.Extensions.Configuration;

namespace SwiftXP.SPT.TheModfather.Updater.Utilities;

public static class CommandLineParameterUtility
{
    private static readonly IConfigurationRoot s_config;

    static CommandLineParameterUtility()
    {
        s_config = new ConfigurationBuilder()
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();
    }

    public static int? GetProcessId()
    {
        string? pidString = s_config[Constants.ProcessIdParameter];

        return int.TryParse(pidString, out int pid) ? pid : null;
    }

    public static bool IsSilent() => s_config[Constants.SilentParameter] == "true";
}