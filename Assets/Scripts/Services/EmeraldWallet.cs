namespace Services
{
    /// <summary>
    /// Персистентная мета-валюта (эмеральды): в отличие от CoinService (монеты внутри уровня),
    /// живёт в облачном сейве и доступна между уровнями и в Menu. Статикой, чтобы UI
    /// без DI (Menu) и DI-сервисы в GameScene читали один и тот же баланс.
    /// </summary>
    public static class EmeraldWallet
    {
        public static int Balance => SaveSystem.SaveData.Emeralds;

        public static void Add(int amount)
        {
            if (amount <= 0)
                return;

            SaveSystem.SaveData.Emeralds += amount;
            SaveSystem.Instance.SaveToStorage();
        }

        public static bool TrySpend(int amount)
        {
            if (amount <= 0 || SaveSystem.SaveData.Emeralds < amount)
                return false;

            SaveSystem.SaveData.Emeralds -= amount;
            SaveSystem.Instance.SaveToStorage();
            return true;
        }
    }
}
