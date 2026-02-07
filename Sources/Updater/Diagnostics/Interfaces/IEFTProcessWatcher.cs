namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public interface IEFTProcessWatcher
{
    Task<bool> WaitForProcessToCloseAsync(CancellationToken cancellationToken);
}