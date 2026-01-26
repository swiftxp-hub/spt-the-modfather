namespace SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

public interface IProcessWatcher
{
    Task<bool> WaitForEftProcessToCloseAsync();
}
