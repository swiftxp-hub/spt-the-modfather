using SwiftXP.SPT.TheModfather.Updater.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Logging;

namespace SwiftXP.SPT.TheModfather.Updater.Utilities;

public static class UpdaterUtility
{


    public static async Task<bool> UpdateModsAsync()
    {
        StaticLog.WriteMessage("Starting update...");

        try
        {
            if (CheckPreRequirements(out string? basePath, out string? payloadPath))
            {
                bool eftIsClosed = await ProcessWatcher.WaitForEftProcessToCloseAsync();
                if (eftIsClosed)
                {
                    DeleteUtility.ProcessDeleteInstructions(basePath!, payloadPath!, Constants.DeleteInstructionSuffix);
                    FileMoverUtility.MoveFiles(basePath!, payloadPath!);

                    StaticLog.WriteMessage("Update completed");

                    await CleanUpPayloadDirectory(payloadPath!);

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            StaticLog.WriteError("Unexpected error occured", ex);
        }

        StaticLog.WriteMessage("Update failed");

        return false;
    }

    private static bool CheckPreRequirements(out string? basePath, out string? payloadPath)
    {
        basePath = null;
        payloadPath = null;

        if (!File.Exists(Constants.EscapeFromTarkovExe))
        {
            StaticLog.WriteError($"{Constants.EscapeFromTarkovExe} could not be found. Make sure you are running the updater from your SPT folder. Exiting...");

            return false;
        }

        basePath = Path.GetFullPath(AppContext.BaseDirectory);
        payloadPath = Path.Combine(basePath, Constants.PayloadPath);

        if (!Directory.Exists(payloadPath))
        {
            StaticLog.WriteError("Payload directory could not be found. Something went wrong. Exiting...");

            return false;
        }

        return true;
    }

    private static async Task CleanUpPayloadDirectory(string payloadPath)
    {
        await Task.Delay(200);

        StaticLog.WriteMessage($"Cleaning payload directory: {payloadPath}");

        Directory.Delete(payloadPath, true);
        Directory.CreateDirectory(payloadPath);

        StaticLog.WriteMessage($"Cleaned payload directory: {payloadPath}");
    }
}
