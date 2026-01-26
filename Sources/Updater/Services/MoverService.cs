using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class MoverService(ILogService logService) : IMoverService
{
    public void MoveFiles(string basePath, string payloadPath)
    {
        logService.WriteMessage("Moving files...");

        string[] filePaths = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories);

        logService.WriteMessage($"Found {filePaths.Length} file(s) to be moved");

        int counter = 0;
        foreach (string sourceFilePath in filePaths)
        {
            string relativePath = Path.GetRelativePath(payloadPath, sourceFilePath);
            string targetFilePath = Path.Combine(basePath, relativePath);

            string? directoryPath = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            logService.WriteMessage($"Moving file '{sourceFilePath}' to '{targetFilePath}'...");

            File.Move(sourceFilePath, targetFilePath, true);
            counter++;
        }

        if(counter > 0)
            logService.WriteMessage($"Moved {counter} files");
    }
}
