using SwiftXP.SPT.TheModfather.Updater.Logging;

namespace SwiftXP.SPT.TheModfather.Updater.Utilities;

public static class DeleteUtility
{
    public static void ProcessDeleteInstructions(string basePath, string payloadPath, string deleteInstructionSuffix)
    {
        StaticLog.WriteMessage("Processing delete instructions...");

        string[] instructionFiles = Directory.GetFiles(payloadPath, "*" + deleteInstructionSuffix, SearchOption.AllDirectories);

        StaticLog.WriteMessage($"{instructionFiles.Length} delete instructions found");

        int counter = 0;
        foreach (string instructionFile in instructionFiles)
        {
            string relativePathWithSuffix = Path.GetRelativePath(payloadPath, instructionFile);
            string relativeTargetPathWithoutSuffix = relativePathWithSuffix.Substring(0, relativePathWithSuffix.Length - deleteInstructionSuffix.Length);

            string targetPath = Path.Combine(basePath, relativeTargetPathWithoutSuffix);

            StaticLog.WriteMessage($"Deleting file: {targetPath}");

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                counter++;

                StaticLog.WriteMessage($"Deleted file: {targetPath}");
            }
            else
            {
                StaticLog.WriteMessage($"File did not exist (unexpected): {targetPath}");
            }

            // Not ready for prod for now...
            // CleanUpIfEmptyDirectory(targetPath);

            StaticLog.WriteMessage($"Deleting instructions file: {instructionFile}");

            File.Delete(instructionFile);

            StaticLog.WriteMessage($"Deleted instructions file: {instructionFile}");
        }

        if (counter > 0)
            StaticLog.WriteMessage($"Delete instructions processed. Deleted {counter} files");
    }

    private static void CleanUpIfEmptyDirectory(string targetPath)
    {
        string? parentPath = Path.GetDirectoryName(targetPath);
        if (parentPath != null)
        {
            bool isEmpty = !Directory.EnumerateFiles(parentPath, "*", SearchOption.AllDirectories).Any();
            if (isEmpty)
            {
                StaticLog.WriteMessage($"Deleting directory (is empty): {parentPath}");

                Directory.Delete(parentPath, true);

                StaticLog.WriteMessage($"Deleted directory: {parentPath}");
            }
        }
    }
}
