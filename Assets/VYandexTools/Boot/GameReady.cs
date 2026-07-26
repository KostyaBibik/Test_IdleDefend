using System;
using Kimicu.YandexGames;
using UnityEngine;

namespace DefaultNamespace.Yandex
{
    public class GameReady : MonoBehaviour
    {
        private void Awake() => YandexGamesSdk.GameReady();
    }
}