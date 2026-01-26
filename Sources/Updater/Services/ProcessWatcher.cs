using System.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class ProcessWatcher(ILogService logService) : IProcessWatcher
{
    public async Task<bool> WaitForEftProcessToCloseAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(60);

        logService.Write("Waiting for EFT-Process to be closed...");

        int? processId = GetEftProcessId();
        if (!processId.HasValue)
        {
            logService.Write("No EFT-Process ID found via Args or Name. Assuming closed.");
            
            return true;
        }

        logService.Write($"EFT-Process ID '{processId}' found. Waiting max {timeout.TotalSeconds}s...");

        try
        {
            Process process = Process.GetProcessById(processId.Value);
            Stopwatch sw = Stopwatch.StartNew();

            while (!process.HasExited)
            {
                if (sw.Elapsed > timeout)
                {
                    return false;
                }

                await Task.Delay(500);
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
        catch (Exception ex)
        {
            logService.Error("Unexpected error while waiting for process exit.", ex);

            return false;
        }
    }

    private int? GetEftProcessId()
    {
        int? eftProcessId = CommandLineParameterService.GetProcessId();

        if (eftProcessId.HasValue)
        {
            logService.Write($"EFT-Process ID from cmd-parameter: {eftProcessId}");
            return eftProcessId;
        }

        Process[] tarkovProcesses = Process.GetProcessesByName("EscapeFromTarkov");
        if (tarkovProcesses.Length > 0)
        {
            logService.Write($"EFT-Process ID from GetProcessesByName: {tarkovProcesses[0].Id}");
            return tarkovProcesses[0].Id;
        }

        Process[] beProcesses = Process.GetProcessesByName("EscapeFromTarkov_BE");
        if (beProcesses.Length > 0)
        {
            logService.Write($"EFT-Process ID from GetProcessesByName (BE): {beProcesses[0].Id}");
            return beProcesses[0].Id;
        }

        return null;
    }
}
