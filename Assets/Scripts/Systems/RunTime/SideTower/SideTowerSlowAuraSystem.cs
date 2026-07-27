using System.Collections.Generic;
using Enums;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.SideTower
{
    public class SideTowerSlowAuraSystem : ITickable
    {
        private readonly SideTowerService _sideTowerService;
        private readonly EnemyService _enemyService;

        public SideTowerSlowAuraSystem(
            SideTowerService sideTowerService,
            EnemyService enemyService
        )
        {
            _sideTowerService = sideTowerService;
            _enemyService = enemyService;
        }

        public void Tick()
        {
            var enemies = _enemyService.Enemies;

            foreach (var enemy in enemies)
            {
                enemy.speedMultiplier = 1f;
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
