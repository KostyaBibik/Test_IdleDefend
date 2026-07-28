using Services;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    /// <summary>
    /// Дебаг-начисление эмеральдов через Tools > Cheats. Работает только в Play Mode:
    /// EmeraldWallet ходит в SaveSystem -> Cloud, который инициализируется исключительно
    /// в сцене Boot (см. Boot.cs, Cloud.Initialize() в его Start()). Если запустить Play
    /// прямо со сцены Menu/GameScene в обход Boot, сейв не готов — ловим это и пишем
    /// понятную причину вместо сырого стектрейса.
    /// </summary>
    public static class CheatMenu
    {
        private const int SmallAmount = 1000;
        private const int LargeAmount = 10000;

        private const string SmallMenuPath = "Tools/Cheats/Add 1000 Emeralds";
        private const string LargeMenuPath = "Tools/Cheats/Add 10000 Emeralds";

        [MenuItem(SmallMenuPath, priority = 100)]
        private static void AddSmallEmeralds() => AddEmeralds(SmallAmount);

        [MenuItem(SmallMenuPath, true)]
        private static bool ValidateAddSmallEmeralds() => Application.isPlaying;

        [MenuItem(LargeMenuPath, priority = 101)]
        private static void AddLargeEmeralds() => AddEmeralds(LargeAmount);

        [MenuItem(LargeMenuPath, true)]
        private static bool ValidateAddLargeEmeralds() => Application.isPlaying;

        private static void AddEmeralds(int amount)
        {
            try
            {
                EmeraldWallet.Add(amount);
                Debug.Log($"[Cheat] +{amount} эмеральдов. Баланс: {EmeraldWallet.Balance}");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[Cheat] Не удалось начислить: сейв ещё не готов. Запусти Play со сцены " +
                                "Boot (не Menu/GameScene напрямую) и дождись загрузки, потом жми чит. " +
                                $"Исходная ошибка: {e.Message}");
            }
        }
    }
}
