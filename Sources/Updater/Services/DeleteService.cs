using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class DeleteService(ILogService logService) : IDeleteService
{
    public void ProcessDeleteInstructions(string basePath, string payloadPath, string deleteInstructionSuffix)
    {
        logService.WriteMessage("Processing delete instructions...");

        string[] instructionFiles = Directory.GetFiles(payloadPath, "*" + deleteInstructionSuffix, SearchOption.AllDirectories);

        logService.WriteMessage($"{instructionFiles.Length} delete instructions found");

        int counter = 0;
        foreach (string instructionFile in instructionFiles)
        {
            string relativePathWithSuffix = Path.GetRelativePath(payloadPath, instructionFile);
            string relativeTargetPathWithoutSuffix = relativePathWithSuffix.Substring(0, relativePathWithSuffix.Length - deleteInstructionSuffix.Length);

            string targetPath = Path.Combine(basePath, relativeTargetPathWithoutSuffix);

            logService.WriteMessage($"Deleting file: {targetPath}");

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                counter++;

                logService.WriteMessage($"Deleted file: {targetPath}");
            }
            else
            {
                logService.WriteMessage($"File did not exist (unexpected): {targetPath}");
            }

            // Not ready for prod for now...
            // CleanUpIfEmptyDirectory(targetPath);

            logService.WriteMessage($"Deleting instructions file: {instructionFile}");

            File.Delete(instructionFile);

            logService.WriteMessage($"Deleted instructions file: {instructionFile}");
        }

        if (counter > 0)
            logService.WriteMessage($"Delete instructions processed. Deleted {counter} files");
    }

    private void CleanUpIfEmptyDirectory(string targetPath)
    {
        string? parentPath = Path.GetDirectoryName(targetPath);
        if (parentPath != null)
        {
            bool isEmpty = !Directory.EnumerateFiles(parentPath, "*", SearchOption.AllDirectories).Any();
            if (isEmpty)
            {
                logService.WriteMessage($"Deleting directory (is empty): {parentPath}");

                Directory.Delete(parentPath, true);

                logService.WriteMessage($"Deleted directory: {parentPath}");
            }
        }
    }
}
