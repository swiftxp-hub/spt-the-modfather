using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class UpdaterService(ILogService logService, IProcessWatcher processWatcher, IDeleteService deleteService, IMoverService moverService) : IUpdaterService
{
    private const string EscapeFromTarkovExe = "EscapeFromTarkov.exe";
    private const string ModfatherDataPath = "TheModfather_Data";
    private const string PayloadPath = "Payload";
    private const string DeleteInstructionSuffix = ".delete";

    public async Task<bool> Update()
    {
        logService.Write("Starting update...");

        try
        {
            if (CheckPreRequirements(out string? basePath, out string? payloadPath))
            {
                bool eftIsClosed = await processWatcher.WaitForEftProcessToCloseAsync();
                if (eftIsClosed)
                {
                    deleteService.ProcessDeleteInstructions(basePath!, payloadPath!, DeleteInstructionSuffix);
                    moverService.MoveFiles(basePath!, payloadPath!);

                    logService.Write("Update completed.");

                    await CleanUpPayloadDirectory(payloadPath!);

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            logService.Error("Unexpected error occured.", ex);
        }

        logService.Write("Update failed.");

        return false;
    }

    private bool CheckPreRequirements(out string? basePath, out string? payloadPath)
    {
        basePath = null;
        payloadPath = null;

        if (!File.Exists(EscapeFromTarkovExe))
        {
            logService.Error($"{EscapeFromTarkovExe} could not be found. Make sure you are running the updater from your SPT folder. Exiting...");

            return false;
        }

        basePath = Path.GetFullPath(AppContext.BaseDirectory);
        payloadPath = Path.Combine(basePath, ModfatherDataPath, PayloadPath);

        if (!Directory.Exists(payloadPath))
        {
            logService.Error("Payload directory could not be found. Something went wrong. Exiting...");

            return false;
        }

        return true;
    }

    private async Task CleanUpPayloadDirectory(string payloadPath)
    {
        await Task.Delay(200);

        logService.Write($"Cleaning payload directory: {payloadPath}");

        Directory.Delete(payloadPath, true);
        Directory.CreateDirectory(payloadPath);

        logService.Write($"Cleaned payload directory: {payloadPath}");
    }
}
