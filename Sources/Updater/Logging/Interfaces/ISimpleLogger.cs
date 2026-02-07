namespace SwiftXP.SPT.TheModfather.Updater.Logging;

public interface ISimpleLogger
{
    Task StartNewFile(CancellationToken cancellationToken);

    Task WriteMessageAsync(string message, CancellationToken cancellationToken);

    Task WriteErrorAsync(string message, CancellationToken cancellationToken);

    Task WriteErrorAsync(string message, Exception exception, CancellationToken cancellationToken);
}