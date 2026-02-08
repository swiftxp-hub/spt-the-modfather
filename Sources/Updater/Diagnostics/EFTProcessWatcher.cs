using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Updater.Environment;
using SwiftXP.SPT.TheModfather.Updater.Logging;

namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public class EFTProcessWatcher(ISimpleLogger simpleLogger,
    ICommandLineArgsReader commandLineArgsReader,
    IProcessService processService) : IEFTProcessWatcher
{
    public async Task<bool> WaitForProcessToCloseAsync(CancellationToken cancellationToken = default)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(60);

        await simpleLogger.WriteMessageAsync("Waiting for EFT-Process to be closed...", cancellationToken);

        int? processId = await GetEftProcessId(cancellationToken);
        if (!processId.HasValue)
        {
            await simpleLogger.WriteMessageAsync("No EFT-Process ID found via args or name. Assuming closed", cancellationToken);

            return true;
        }

        await simpleLogger.WriteMessageAsync($"EFT-Process ID '{processId}' found. Waiting max {timeout.TotalSeconds}s...", cancellationToken);

        try
        {
            IProcessWrapper? process = processService.GetProcessById(processId.Value);
            if (process == null)
                return true;

            Stopwatch sw = Stopwatch.StartNew();

            while (!process.HasExited)
            {
                if (sw.Elapsed > timeout)
                    return false;

                await Task.Delay(500, cancellationToken);
                process.Refresh();
            }

            return true;
        }
        catch (ArgumentException)
        {
            return true;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private async Task<int?> GetEftProcessId(CancellationToken cancellationToken = default)
    {
        int? eftProcessId = commandLineArgsReader.GetProcessId();
        if (eftProcessId.HasValue)
        {
            await simpleLogger.WriteMessageAsync($"EFT-Process ID from commandline-parameter: {eftProcessId}", cancellationToken);

            return eftProcessId;
        }

        IProcessWrapper[] tarkovProcesses = processService.GetProcessesByName("EscapeFromTarkov");
        if (tarkovProcesses.Length > 0)
        {
            await simpleLogger.WriteMessageAsync($"EFT-Process ID from GetProcessesByName: {tarkovProcesses[0].Id}", cancellationToken);

            return tarkovProcesses[0].Id;
        }

        IProcessWrapper[] beProcesses = processService.GetProcessesByName("EscapeFromTarkov_BE");
        if (beProcesses.Length > 0)
        {
            await simpleLogger.WriteMessageAsync($"EFT-Process ID from GetProcessesByName (BE): {beProcesses[0].Id}", cancellationToken);

            return beProcesses[0].Id;
        }

        return null;
    }
}
