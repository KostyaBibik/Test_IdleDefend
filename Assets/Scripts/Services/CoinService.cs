using System;
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
        private const int StartCoins = 400;

        private int countCoins;

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
            countCoins = StartCoins;
            onUpdateCountCoins?.Invoke(countCoins);
        }
    }
}
