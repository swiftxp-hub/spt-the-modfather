using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SwiftXP.SPT.TheModfather.Updater.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Logging;

namespace SwiftXP.SPT.TheModfather.Updater.Services;

public class UpdateManager(ISimpleLogger simpleLogger,
    IEFTProcessWatcher eftProcessWatcher) : IUpdateManager
{
    public async Task ProcessUpdatesAsync(IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        await simpleLogger.WriteMessageAsync("Starting update...", cancellationToken);

        if (CheckPreRequirements(out string? baseDirectory, out string? stagingDirectory))
        {
            bool eftIsClosed = await eftProcessWatcher.WaitForProcessToCloseAsync(cancellationToken);
            if (eftIsClosed)
            {
                string[] totalFiles = Directory.GetFiles(stagingDirectory!, "*", SearchOption.AllDirectories);
                if (totalFiles.Length > 0)
                {
                    int processedFiles = 0;

                    await ProcessDeleteInstructions(baseDirectory!, stagingDirectory!, () =>
                    {
                        progress.Report((int)Math.Round((double)++processedFiles / totalFiles.Length * 100));
                    }, cancellationToken);

                    await MoveStagingFilesAsync(baseDirectory!, stagingDirectory!, () =>
                    {
                        progress.Report((int)Math.Round((double)++processedFiles / totalFiles.Length * 100));
                    }, cancellationToken);

                    await FinalizeManifestAsync(baseDirectory!, stagingDirectory!, cancellationToken);
                    progress.Report(100);

                    await CleanUpPayloadDirectory(stagingDirectory!, cancellationToken);
                }
            }
        }

        await simpleLogger.WriteMessageAsync("Update finished.", cancellationToken);
    }

    private static bool CheckPreRequirements(out string? baseDirectory, out string? stagingDirectory)
    {
        baseDirectory = null;
        stagingDirectory = null;

        if (!File.Exists(Constants.EscapeFromTarkovExe))
        {
            throw new InvalidOperationException(
                $"{Constants.EscapeFromTarkovExe} could not be found. Make sure you are running the updater from your SPT folder.");
        }

        baseDirectory = Path.GetFullPath(AppContext.BaseDirectory);
        stagingDirectory = Path.Combine(baseDirectory, Constants.ModfatherDataDirectory, Constants.StagingDirectory);

        if (!Directory.Exists(stagingDirectory))
        {
            throw new InvalidOperationException(
                $"Staging directory could not be found. Something went wrong.");
        }

        return true;
    }

    private async Task ProcessDeleteInstructions(string baseDirectory, string stagingDirectory, Action onDeletedFile, CancellationToken cancellationToken = default)
    {
        await simpleLogger.WriteMessageAsync("Processing delete instructions...", cancellationToken);

        string[] instructionFiles = Directory.GetFiles(stagingDirectory, $"*{Constants.DeleteInstructionSuffix}", SearchOption.AllDirectories);

        await simpleLogger.WriteMessageAsync($"{instructionFiles.Length} delete instructions found", cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        int counter = 0;
        foreach (string instructionFile in instructionFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string relativePathWithSuffix = Path.GetRelativePath(stagingDirectory, instructionFile);
            string relativeTargetPathWithoutSuffix = relativePathWithSuffix[..^Constants.DeleteInstructionSuffix.Length];
            string targetPath = Path.Combine(baseDirectory, relativeTargetPathWithoutSuffix);

            await simpleLogger.WriteMessageAsync($"Deleting file: {targetPath}", cancellationToken);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);

                await simpleLogger.WriteMessageAsync($"Deleted file: {targetPath}", cancellationToken);
            }
            else
            {
                await simpleLogger.WriteMessageAsync($"File did not exist (unexpected): {targetPath}", cancellationToken);
            }

            await simpleLogger.WriteMessageAsync($"Deleting instructions file: {instructionFile}", cancellationToken);

            File.Delete(instructionFile);

            counter++;
            onDeletedFile();

            await simpleLogger.WriteMessageAsync($"Deleted instructions file: {instructionFile}", cancellationToken);
        }

        if (counter > 0)
            await simpleLogger.WriteMessageAsync($"Delete instructions processed. Deleted {counter} files", cancellationToken);
    }

    private async Task MoveStagingFilesAsync(string baseDirectory, string stagingDirectory, Action onMovedFile, CancellationToken cancellationToken = default)
    {
        await simpleLogger.WriteMessageAsync("Moving files...", cancellationToken);

        string[] filePaths = Directory.GetFiles(stagingDirectory, "*", SearchOption.AllDirectories);
        string manifestRelativePath = Path.Combine(Constants.ModfatherDataDirectory, Constants.ClientManifestFile);

        await simpleLogger.WriteMessageAsync($"Found {filePaths.Length} file(s) to be moved", cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        int counter = 0;
        foreach (string sourceFilePath in filePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string relativePath = Path.GetRelativePath(stagingDirectory, sourceFilePath);
            if (relativePath.Equals(manifestRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                await simpleLogger.WriteMessageAsync($"Skipping manifest file for final move: {sourceFilePath}", cancellationToken);
                continue;
            }

            string targetFilePath = Path.Combine(baseDirectory, relativePath);

            string? directoryPath = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(directoryPath))
                Directory.CreateDirectory(directoryPath);

            await simpleLogger.WriteMessageAsync($"Moving file '{sourceFilePath}' to '{targetFilePath}'...", cancellationToken);

            File.Move(sourceFilePath, targetFilePath, true);

            counter++;
            onMovedFile();
        }

        if (counter > 0)
            await simpleLogger.WriteMessageAsync($"Moved {counter} files", cancellationToken);
    }

    private async Task FinalizeManifestAsync(string baseDirectory, string stagingDirectory, CancellationToken cancellationToken)
    {
        string manifestRelativePath = Path.Combine(Constants.ModfatherDataDirectory, Constants.ClientManifestFile);
        string sourcePath = Path.Combine(stagingDirectory, manifestRelativePath);
        string targetPath = Path.Combine(baseDirectory, manifestRelativePath);

        if (File.Exists(sourcePath))
        {
            await simpleLogger.WriteMessageAsync($"Finalizing update: Moving manifest to {targetPath}", cancellationToken);

            string? targetDir = Path.GetDirectoryName(targetPath);
            if (targetDir != null)
                Directory.CreateDirectory(targetDir);

            File.Move(sourcePath, targetPath, true);
        }
        else
        {
            await simpleLogger.WriteMessageAsync("Warning: Staged manifest not found during finalization.", cancellationToken);
        }
    }

    private async Task CleanUpPayloadDirectory(string stagingDirectory, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        await simpleLogger.WriteMessageAsync($"Cleaning payload directory: {stagingDirectory}", cancellationToken);

        Directory.Delete(stagingDirectory, true);
        Directory.CreateDirectory(stagingDirectory);

        await simpleLogger.WriteMessageAsync($"Cleaned payload directory: {stagingDirectory}", cancellationToken);
    }
}
