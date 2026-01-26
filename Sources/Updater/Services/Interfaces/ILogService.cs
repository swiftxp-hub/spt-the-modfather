namespace SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

public interface ILogService
{
    void StartNewFile();

    void WriteError(string message, Exception ex);

    void WriteError(string message);

    void WriteMessage(string message);
}
