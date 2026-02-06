using HarmonyLib;
using System;
using System.Reflection;
using BepInEx.Logging;

namespace SwiftXP.SPT.TheModfather.Client.Patches;

public static class GameStartPatch
{
    private static object? s_tarkovAppInstance;
    private static ManualLogSource? s_logger;

    public static bool IsConfirmed { get; private set; }

    public static void Initialize(ManualLogSource logger)
    {
        s_logger = logger;

        Harmony harmony = new("com.swiftxp.spt.themodfather.blocker_patch");

        Type appType = AccessTools.TypeByName("EFT.TarkovApplication");
        MethodInfo startMethod = AccessTools.Method(appType, "Start");

        if (startMethod != null)
        {
            MethodInfo prefixMethod = typeof(GameStartPatch).GetMethod(nameof(StartPrefix), BindingFlags.Public | BindingFlags.Static);

            HarmonyMethod harmonyPrefix = new(prefixMethod) { priority = Priority.High };
            harmony.Patch(startMethod, harmonyPrefix);
        }
        else
        {
            s_logger.LogError("Critical: Could not find EFT.TarkovApplication.Start!");
        }
    }

    public static void ResumeGame()
    {
        IsConfirmed = true;

        if (s_tarkovAppInstance != null)
        {
            try
            {
                Type appType = AccessTools.TypeByName("EFT.TarkovApplication");

                MethodInfo startMethod = AccessTools.Method(appType, "Start");
                startMethod.Invoke(s_tarkovAppInstance, null);
            }
            catch (Exception ex)
            {
                s_logger?.LogError($"Failed to resume game load: {ex}");
            }
        }
    }

#pragma warning disable CA1707 // Identifiers should not contain underscores

    public static bool StartPrefix(object __instance)
#pragma warning restore CA1707 // Identifiers should not contain underscores

    {
        if (IsConfirmed)
            return true;

        s_tarkovAppInstance = __instance;

        return false;
    }
}