using System.Collections.Generic;
using Enums;
using Services;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime
{
    /// <summary>
    /// Единственный владелец EnemyView.speedMultiplier. Комбинирует два независимых источника
    /// замедления: аура доп-башен SlowAura (пересчитывается с нуля каждый тик по дальности)
    /// и тайм-эффект Frost с главной башни/её ультимейта "Заморозка" (таймер, тикает через
    /// IGameTimeProvider — переживает паузу так же, как и остальной бой). Раньше оба
    /// эффекта пытались бы независимо сбрасывать/накладывать одно и то же поле в двух
    /// разных ITickable без гарантии порядка — здесь одна точка правды на тик.
    /// </summary>
    public class EnemySpeedModifierSystem : ITickable
    {
        private readonly SideTowerService _sideTowerService;
        private readonly EnemyService _enemyService;
        private readonly IGameTimeProvider _gameTimeProvider;

        public EnemySpeedModifierSystem(
            SideTowerService sideTowerService,
            EnemyService enemyService,
            IGameTimeProvider gameTimeProvider
        )
        {
            _sideTowerService = sideTowerService;
            _enemyService = enemyService;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Tick()
        {
            var enemies = _enemyService.Enemies;
            var deltaTime = _gameTimeProvider.DeltaTime;

            foreach (var enemy in enemies)
            {
                if (enemy.frostTimeRemaining > 0f)
                {
                    enemy.frostTimeRemaining -= deltaTime;
                    var frostActive = enemy.frostTimeRemaining > 0f;
                    enemy.speedMultiplier = frostActive ? enemy.frostSpeedMultiplier : 1f;
                    enemy.SetFrostVisual(frostActive, enemy.frostSpeedMultiplier);
                }
                else
                {
                    enemy.speedMultiplier = 1f;
                    enemy.SetFrostVisual(false, 1f);
                }
            }

            foreach (var tower in _sideTowerService.Towers)
            {
                if (tower.attackType != ESideTowerAttackType.SlowAura)
                    continue;

                ApplyAura(tower, enemies);
            }
        }

        private static void ApplyAura(SideTowerView tower, List<EnemyView> enemies)
        {
            var towerPos = tower.transform.position;
            var candidateMultiplier = Mathf.Clamp01(1f - tower.slowPercent);

            foreach (var enemy in enemies)
            {
                if (Vector3.Distance(towerPos, enemy.transform.position) > tower.attackDistance)
                    continue;

                if (candidateMultiplier < enemy.speedMultiplier)
                    enemy.speedMultiplier = candidateMultiplier;
            }
        }
    }
}
