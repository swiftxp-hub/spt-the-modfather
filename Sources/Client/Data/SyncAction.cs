using SwiftXP.SPT.TheModfather.Client.Enums;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public class SyncAction
{
    public string RelativeFilePath { get; set; } = string.Empty;

    public SyncActionType Type { get; set; }

    public bool IsSelected { get; set; } = true;
}