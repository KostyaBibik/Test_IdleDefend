using Services;
using Services.Impl;
using UnityEngine;
using Zenject;

namespace Systems.RunTime.Enemies
{
    /// <summary>
    /// Единственный владелец периодического урона от яда (EnemyView.poison*). Аналог
    /// EnemySpeedModifierSystem для фроста: тикает таймер через IGameTimeProvider (переживает
    /// паузу так же, как остальной бой), наносит урон по интервалу и синхронизирует партикл.
    /// </summary>
    public class EnemyPoisonSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly IGameTimeProvider _gameTimeProvider;

        public EnemyPoisonSystem(EnemyService enemyService, IGameTimeProvider gameTimeProvider)
        {
            _enemyService = enemyService;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Tick()
        {
            var deltaTime = _gameTimeProvider.DeltaTime;

            foreach (var enemy in _enemyService.Enemies)
            {
                if (enemy.isDestroyed)
                    continue;

                if (enemy.poisonTimeRemaining <= 0f)
                {
                    enemy.SetPoisonVisual(false);
                    continue;
                }

                enemy.poisonTimeRemaining -= deltaTime;
                enemy.poisonTickRemaining -= deltaTime;

                if (enemy.poisonTickRemaining <= 0f)
                {
                    enemy.poisonTickRemaining += enemy.poisonTickInterval;
                    enemy.healthComponent.ReduceHealth(Mathf.RoundToInt(enemy.poisonDamagePerTick));
                }

                enemy.SetPoisonVisual(enemy.poisonTimeRemaining > 0f);
            }
        }
    }
}
