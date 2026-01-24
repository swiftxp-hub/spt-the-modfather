using System;
using System.Collections;
using System.Collections.Generic;

namespace SwiftXP.SPT.TheModfather.Client.Services.Interfaces;

public interface IModSyncService
{
    void ShowUpdateNotification(Dictionary<string, ModSyncActionEnum> modSyncActions);

    IEnumerator SyncMods(Action<Dictionary<string, ModSyncActionEnum>> onCompleted);

    IEnumerator UpdateModsCoroutine(Dictionary<string, ModSyncActionEnum> modSyncActions);
}