using System;

namespace VYandexTools.Review.Scripts
{
    /// <summary>
    /// Лёгкий статический мост между игровыми панелями (Win/Lose, живут в Zenject-контейнере
    /// GameScene) и ReviewController (DontDestroyOnLoad-синглтон вне DI). Через SignalBus
    /// ReviewController подписаться не может — контейнер пересоздаётся при каждой загрузке сцены.
    /// </summary>
    public static class ReviewTriggerEvents
    {
        public static event Action OnGameSessionEnded;

        public static void RaiseGameSessionEnded() => OnGameSessionEnded?.Invoke();
    }
}
