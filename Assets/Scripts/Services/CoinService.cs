using System;
using Zenject;

namespace Services
{
    public class CoinService : IInitializable
    {
        private const int StartCoins = 400;

        private int countCoins;

        public Action<int> onUpdateCountCoins;

        public void AddCoins(int count)
        {
            countCoins += count;
            SaveSystem.SaveData.Money = countCoins;
            onUpdateCountCoins?.Invoke(countCoins);
        }

        public bool TryBought(int count)
        {
            if (countCoins >= count)
            {
                countCoins -= count;
                SaveSystem.SaveData.Money = countCoins;
                SaveSystem.Instance.SaveToStorage();
                onUpdateCountCoins?.Invoke(countCoins);
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Initialize()
        {
            if (!SaveSystem.SaveData.ProgressInitialized)
            {
                SaveSystem.SaveData.ProgressInitialized = true;
                SaveSystem.SaveData.Money = StartCoins;
                SaveSystem.Instance.SaveToStorage();
            }

            countCoins = SaveSystem.SaveData.Money;
            onUpdateCountCoins?.Invoke(countCoins);
        }
    }
}
