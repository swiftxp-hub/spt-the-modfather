using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public static class FinishUpdateService
{   
    public static async Task<bool> FinishAsync()
    {
        SimpleLogService.Write("Starting finalization of update(s)...");

        try 
        {
            bool closed = await WaitForEftProcessIsClosedAsync(TimeSpan.FromSeconds(60));
            if (!closed)
            {
                SimpleLogService.Error("EFT process did not close in time. Aborting update to prevent file locks.");
                return false;
            }

            SimpleLogService.Write("EFT-process is stopped.");

            string baseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
            string modfatherDataPayloadDirectory = Path.Combine(baseDirectory, "TheModfather_Data", "Payload");

            SimpleLogService.Write($"Directory for payload is: {modfatherDataPayloadDirectory}");

            if(Directory.Exists(modfatherDataPayloadDirectory))
            {
                string[] filePaths = Directory.GetFiles(modfatherDataPayloadDirectory, "*", SearchOption.AllDirectories);
                
                SimpleLogService.Write($"Found {filePaths.Length} file(s) to be moved...");

                foreach(string sourceFilePath in filePaths)
                {
                    string relativePath = Path.GetRelativePath(modfatherDataPayloadDirectory, sourceFilePath);
                    string targetFilePath = Path.GetFullPath(Path.Combine(baseDirectory, relativePath));

                    string? directoryPath = Path.GetDirectoryName(targetFilePath);
                    if (!string.IsNullOrEmpty(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    SimpleLogService.Write($"Moving file to '{relativePath}'...");

                    try 
                    {
                        File.Move(sourceFilePath, targetFilePath, true);
                    }
                    catch (Exception ex)
                    {
                        SimpleLogService.Error($"Failed to move file to '{targetFilePath}'.", ex);

                        return false;
                    }
                }

                SimpleLogService.Write($"Cleaning up payload directory...");

                try 
                {
                    await Task.Delay(200);

                    Directory.Delete(modfatherDataPayloadDirectory, true);
                    Directory.CreateDirectory(modfatherDataPayloadDirectory);
                }
                catch (Exception ex)
                {
                    SimpleLogService.Write($"Warning: Could not cleanup payload directory: {ex.Message}");
                }
            }
            else
            {
                SimpleLogService.Write("Payload directory not found. Nothing to update.");
            }
        }
        catch(Exception exception)
        {
            SimpleLogService.Error("An error during the finalization of the update(s) occured.", exception);
            
            return false;
        }

        return true;
    }

    private static async Task<bool> WaitForEftProcessIsClosedAsync(TimeSpan timeout)
    {
        SimpleLogService.Write("Waiting for EFT-Process to be closed...");

        int? processId = GetEftProcessId();
        if(!processId.HasValue)
        {
            SimpleLogService.Write("No EFT-Process ID found via Args or Name. Assuming closed.");
            return true;
        }

        SimpleLogService.Write($"EFT-Process ID '{processId}' found. Waiting max {timeout.TotalSeconds}s...");
        try
        {
            Process process = Process.GetProcessById(processId.Value);
            Stopwatch sw = Stopwatch.StartNew();

            while(!process.HasExited)
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
            SimpleLogService.Error("Unexpected error while waiting for process exit.", ex);
            return true; 
        }
    }

    private static int? GetEftProcessId()
    {
        int? eftProcessId = CommandLineParameterService.GetProcessId();

        if (eftProcessId.HasValue)
        {
            SimpleLogService.Write($"EFT-Process ID from cmd-parameter: {eftProcessId}");
            return eftProcessId;
        }

        Process[] tarkovProcesses = Process.GetProcessesByName("EscapeFromTarkov");
        if (tarkovProcesses.Length > 0)
        {
            SimpleLogService.Write($"EFT-Process ID from GetProcessesByName: {tarkovProcesses[0].Id}");
            return tarkovProcesses[0].Id;
        }
        
        Process[] beProcesses = Process.GetProcessesByName("EscapeFromTarkov_BE");
        if (beProcesses.Length > 0)
        {
             SimpleLogService.Write($"EFT-Process ID from GetProcessesByName (BE): {beProcesses[0].Id}");
             return beProcesses[0].Id;
        }

        return null;
    }
}