using System.Diagnostics;

namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public class ProcessWrapper(Process process) : IProcessWrapper
{
    public int Id => process.Id;

    public bool HasExited => process.HasExited;

    public void Refresh()
    {
        process.Refresh();
    }
}