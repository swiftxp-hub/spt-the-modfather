using System;
using System.Diagnostics;
using System.Linq;

namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public class ProcessService : IProcessService
{
    public IProcessWrapper? GetProcessById(int id)
    {
        try
        {
            Process process = Process.GetProcessById(id);

            return new ProcessWrapper(process);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public IProcessWrapper[] GetProcessesByName(string name)
    {
        Process[] processes = Process.GetProcessesByName(name);

        return [.. processes
            .Select(p => new ProcessWrapper(p))
            .Cast<IProcessWrapper>()];
    }
}