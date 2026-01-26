
using System.IO;
using System.Security.Policy;
using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class DeleteService(ILogService logService) : IDeleteService
{
    public void ProcessDeleteInstructions(string basePath, string payloadPath, string deleteInstructionSuffix)
    {
        logService.Write("Processing delete instructions...");

        string[] instructionFiles = Directory.GetFiles(payloadPath, "*" + deleteInstructionSuffix, SearchOption.AllDirectories);

        logService.Write($"{instructionFiles.Length} delete instructions found");

        int counter = 0;
        foreach (string instructionFile in instructionFiles)
        {
            string relativePathWithSuffix = Path.GetRelativePath(payloadPath, instructionFile);
            string relativeTargetPathWithoutSuffix = relativePathWithSuffix.Substring(0, relativePathWithSuffix.Length - deleteInstructionSuffix.Length);

            string targetPath = Path.Combine(basePath, relativeTargetPathWithoutSuffix);

            logService.Write($"Deleting file: {targetPath}");

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
                counter++;

                logService.Write($"Deleted file: {targetPath}");
            }
            else
            {
                logService.Write($"File did not exist (unexpected): {targetPath}");
            }

            // Not ready for prod for now...
            // CleanUpIfEmptyDirectory(targetPath);

            logService.Write($"Deleting instructions file: {instructionFile}");

            File.Delete(instructionFile);

            logService.Write($"Deleted instructions file: {instructionFile}");
        }

        if(counter > 0)
            logService.Write($"Delete instructions processed. Deleted {counter} files");
    }

    private void CleanUpIfEmptyDirectory(string targetPath)
    {
        string? parentPath = Path.GetDirectoryName(targetPath);
        if (parentPath != null)
        {
            bool isEmpty = !Directory.EnumerateFiles(parentPath, "*", SearchOption.AllDirectories).Any();
            if (isEmpty)
            {
                logService.Write($"Deleting directory (is empty): {parentPath}");

                Directory.Delete(parentPath, true);

                logService.Write($"Deleted directory: {parentPath}");
            }
        }
    }
}
