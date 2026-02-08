namespace SwiftXP.SPT.TheModfather.Updater.Diagnostics;

public interface IProcessWrapper
{
    int Id { get; }

    bool HasExited { get; }

    void Refresh();
}