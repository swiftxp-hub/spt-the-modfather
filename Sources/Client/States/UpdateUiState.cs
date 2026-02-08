using SwiftXP.SPT.TheModfather.Client.Data;
using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Client.UI;

public class UpdateUiState
{
    public string StatusText { get; set; } = "Initializing...";

    public float Progress { get; set; }

    public string ProgressHeader { get; set; } = string.Empty;

    public string ProgressDetail { get; set; } = string.Empty;

    public bool IsError { get; set; }

    public IReadOnlyList<SyncAction> SyncActions { get; set; } = [];
}