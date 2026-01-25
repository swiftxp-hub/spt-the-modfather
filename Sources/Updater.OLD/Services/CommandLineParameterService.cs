using System;
using Microsoft.Extensions.Configuration;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public static class CommandLineParameterService
{
    public static int? GetProcessId()
    {
        string[] args = Environment.GetCommandLineArgs();

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        string? pidString = config["processid"];
        if(int.TryParse(pidString, out int pid))
        {
            return pid;
        }

        return null;
    }

    public static bool IsSilent()
    {
        string[] args = Environment.GetCommandLineArgs();

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddCommandLine(args)
            .Build();

        return config["silent"] != null;
    }
}