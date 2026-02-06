using SwiftXP.SPT.TheModfather.Updater.Logging;

namespace SwiftXP.SPT.TheModfather.Updater.Utilities;

public static class FileMoverUtility
{
    public static void MoveFiles(string basePath, string payloadPath)
    {
        StaticLog.WriteMessage("Moving files...");

        string[] filePaths = Directory.GetFiles(payloadPath, "*", SearchOption.AllDirectories);

        StaticLog.WriteMessage($"Found {filePaths.Length} file(s) to be moved");

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

            StaticLog.WriteMessage($"Moving file '{sourceFilePath}' to '{targetFilePath}'...");

            File.Move(sourceFilePath, targetFilePath, true);
            counter++;
        }

        if (counter > 0)
            StaticLog.WriteMessage($"Moved {counter} files");
    }
}
