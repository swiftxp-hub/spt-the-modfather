namespace SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

public interface ILogService
{
    void Error(string message, Exception ex);

    void Error(string message);

    void StartNewFile();

    void Write(string message);
}
