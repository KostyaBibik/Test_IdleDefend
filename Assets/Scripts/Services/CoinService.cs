using System;
using Db;
using Zenject;

namespace Services
{
    /// <summary>
    /// Монеты внутри игровой сессии. Живут только в рамках одного уровня — не сохраняются
    /// в SaveSystem и не переносятся ни в меню, ни на следующий уровень. Персистентная
    /// мета-валюта между сессиями — см. EmeraldWallet.
    /// </summary>
    public class CoinService : IInitializable
    {
        private readonly LevelsConfig _levelsConfig;

        public CoinService(LevelsConfig levelsConfig)
        {
            _levelsConfig = levelsConfig;
        }

        private int countCoins;

        public int CurrentCoins => countCoins;

        public Action<int> onUpdateCountCoins;

        public void AddCoins(int count)
        {
            countCoins += count;
            onUpdateCountCoins?.Invoke(countCoins);
        }

        public bool TryBought(int count)
        {
            if (countCoins >= count)
            {
                countCoins -= count;
                onUpdateCountCoins?.Invoke(countCoins);
                return true;
            }

            return false;
        }

        public void Initialize()
        {
            countCoins = GetStartCoins();
            onUpdateCountCoins?.Invoke(countCoins);
        }

        private int GetStartCoins()
        {
            var levelIndex = SelectedLevelHolder.SelectedLevelIndex;
            if (levelIndex < 0 || levelIndex >= _levelsConfig.Count)
                levelIndex = 0;

            return Math.Max(0, _levelsConfig.GetByIndex(levelIndex).StartCoins);
        }
    }
}
