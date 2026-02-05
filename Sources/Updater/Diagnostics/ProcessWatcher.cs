using System.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Logging;
using SwiftXP.SPT.TheModfather.Updater.Utilities;

namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public static class ProcessWatcher
{
    public static async Task<bool> WaitForEftProcessToCloseAsync()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(60);

        StaticLog.WriteMessage("Waiting for EFT-Process to be closed...");

        int? processId = GetEftProcessId();
        if (!processId.HasValue)
        {
            StaticLog.WriteMessage("No EFT-Process ID found via Args or Name. Assuming closed");

            return true;
        }

        StaticLog.WriteMessage($"EFT-Process ID '{processId}' found. Waiting max {timeout.TotalSeconds}s...");

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
            StaticLog.WriteError("Unexpected error while waiting for process exit", ex);

            return false;
        }
    }

    private static int? GetEftProcessId()
    {
        int? eftProcessId = CommandLineParameterUtility.GetProcessId();

        if (eftProcessId.HasValue)
        {
            StaticLog.WriteMessage($"EFT-Process ID from cmd-parameter: {eftProcessId}");
            return eftProcessId;
        }

        Process[] tarkovProcesses = Process.GetProcessesByName("EscapeFromTarkov");
        if (tarkovProcesses.Length > 0)
        {
            StaticLog.WriteMessage($"EFT-Process ID from GetProcessesByName: {tarkovProcesses[0].Id}");
            return tarkovProcesses[0].Id;
        }

        Process[] beProcesses = Process.GetProcessesByName("EscapeFromTarkov_BE");
        if (beProcesses.Length > 0)
        {
            StaticLog.WriteMessage($"EFT-Process ID from GetProcessesByName (BE): {beProcesses[0].Id}");
            return beProcesses[0].Id;
        }

        return null;
    }
}
