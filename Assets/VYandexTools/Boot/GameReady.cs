using Kimicu.YandexGames;
using UnityEngine;

namespace DefaultNamespace.Yandex
{
    public class GameReady : MonoBehaviour
    {
        private static bool _isSent;

        public static void Notify()
        {
            if (_isSent)
                return;

            YandexGamesSdk.GameReady();
            _isSent = true;
        }
    }
}
