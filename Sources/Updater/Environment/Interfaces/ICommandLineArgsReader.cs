namespace SwiftXP.SPT.TheModfather.Updater.Environment;

public interface ICommandLineArgsReader
{
    int? GetProcessId();

    bool IsSilent();
}