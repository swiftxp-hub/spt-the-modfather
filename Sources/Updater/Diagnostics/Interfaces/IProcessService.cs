namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public interface IProcessService
{
    IProcessWrapper? GetProcessById(int id);

    IProcessWrapper[] GetProcessesByName(string name);
}