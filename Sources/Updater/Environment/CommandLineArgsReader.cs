using Microsoft.Extensions.Configuration;

namespace SwiftXP.SPT.TheModfather.Updater.Environment;

public class CommandLineArgsReader : ICommandLineArgsReader
{
    private readonly IConfigurationRoot _config;

    public CommandLineArgsReader()
    {
        _config = new ConfigurationBuilder()
            .AddCommandLine(System.Environment.GetCommandLineArgs())
            .Build();
    }

    public int? GetProcessId()
    {
        string? pidString = _config[Constants.ProcessIdParameter];

        return int.TryParse(pidString, out int pid) ? pid : null;
    }

    public bool IsSilent() => _config[Constants.SilentParameter] == "true";
}