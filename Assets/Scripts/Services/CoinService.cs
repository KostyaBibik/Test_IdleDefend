using System;
using UnityEngine;
using Zenject;

namespace Services
{
    public class CoinService : IInitializable
    {
        private int countCoins;

        public Action<int> onUpdateCountCoins;

        public void AddCoins(int count)
        {
            Debug.Log($"Add coins {count}");
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
            else
            {
                return false;
            }
        }

        public void Initialize()
        {
            onUpdateCountCoins?.Invoke(countCoins);
        }
    }
}