#if UNITY_EDITOR
using System;
using Installers;
using Services;
using UnityEditor;
using UnityEngine;
using Zenject;

public static class EditorCoinCheatMenu
{
    private const int CoinsAmount = 10000;
    private const string MenuPath = "Tools/IdleDefend/Add 10000 Coins";

    [MenuItem(MenuPath)]
    private static void AddCoins()
    {
        var coinService = ResolveCoinService();
        if (coinService == null)
        {
            Debug.LogWarning("[EditorCoinCheatMenu] CoinService is not available in the active play session.");
            return;
        }

        coinService.AddCoins(CoinsAmount);
        TrySaveToStorage();
        Debug.Log($"[EditorCoinCheatMenu] Added {CoinsAmount} coins.");
    }

    [MenuItem(MenuPath, true)]
    private static bool CanAddCoins()
    {
        return EditorApplication.isPlaying;
    }

    private static CoinService ResolveCoinService()
    {
        var container = DiContainerRef.Container;
        if (container == null)
            return null;

        try
        {
            return container.Resolve<CoinService>();
        }
        catch (ZenjectException)
        {
            return null;
        }
    }

    private static void TrySaveToStorage()
    {
        try
        {
            SaveSystem.Instance.SaveToStorage();
        }
        catch (NullReferenceException)
        {
            Debug.LogWarning("[EditorCoinCheatMenu] Coins were added, but SaveSystem is not available.");
        }
    }
}
#endif
