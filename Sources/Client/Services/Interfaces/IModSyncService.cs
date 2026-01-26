using System;
using System.Collections;
using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

public interface IModSyncService
{
    void ShowUpdateNotification(Dictionary<string, ModSyncAction> modSyncActions);

    IEnumerator SyncMods(Action<Dictionary<string, ModSyncAction>> onCompleted);

    IEnumerator UpdateModsCoroutine(Dictionary<string, ModSyncAction> modSyncActions);
}