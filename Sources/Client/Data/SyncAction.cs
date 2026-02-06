using SwiftXP.SPT.TheModfather.Client.Enums;

namespace SwiftXP.SPT.TheModfather.Client.Data;

public class SyncAction(
    string RelativeFilePath,
    SyncActionType Type,
    string? Hash = null,
    long? SizeInBytes = null,
    bool IsSelected = true)
{
    public string RelativeFilePath { get; } = RelativeFilePath;

    public SyncActionType Type { get; } = Type;

    public string? Hash { get; } = Hash;

    public long? SizeInBytes { get; } = SizeInBytes;

    public bool IsSelected { get; set; } = IsSelected;
}