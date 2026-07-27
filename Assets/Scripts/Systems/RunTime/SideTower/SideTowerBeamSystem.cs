using Enums;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.SideTower
{
    public class SideTowerBeamSystem : ITickable
    {
        private readonly SideTowerService _sideTowerService;
        private readonly EnemyService _enemyService;

        public SideTowerBeamSystem(
            SideTowerService sideTowerService,
            EnemyService enemyService
        )
        {
            _sideTowerService = sideTowerService;
            _enemyService = enemyService;
        }

        public void Tick()
        {
            foreach (var tower in _sideTowerService.Towers)
            {
                if (tower.attackType != ESideTowerAttackType.Beam)
                    continue;

                TickBeam(tower);
            }
        }

        private void TickBeam(SideTowerView tower)
        {
            var towerPos = tower.transform.position;

            if (tower.beamCurrentTarget == null || tower.beamCurrentTarget.isDestroyed ||
                Vector3.Distance(towerPos, tower.beamCurrentTarget.transform.position) > tower.attackDistance)
            {
                tower.beamCurrentTarget = FindNewTarget(towerPos, tower.attackDistance);
                tower.beamDamageAccumulator = 0f;
            }

            if (tower.beamCurrentTarget == null)
            {
                if (tower.BeamLine != null)
                    tower.BeamLine.enabled = false;
                return;
            }

            var maxHealth = tower.beamCurrentTarget.healthComponent.GetMaxHealth();
            tower.beamDamageAccumulator += maxHealth * tower.beamPercentMaxHealthPerSecond * Time.deltaTime;

            if (tower.beamDamageAccumulator >= 1f)
            {
                var toApply = Mathf.FloorToInt(tower.beamDamageAccumulator);
                tower.beamDamageAccumulator -= toApply;
                tower.beamCurrentTarget.healthComponent.ReduceHealth(toApply);
                tower.beamCurrentTarget.healthComponent.ReduceAssumedHealth(toApply);
            }

            if (tower.BeamLine != null)
            {
                tower.BeamLine.enabled = true;
                tower.BeamLine.SetPosition(0, towerPos);
                tower.BeamLine.SetPosition(1, tower.beamCurrentTarget.transform.position);
            }
        }

        private EnemyView FindNewTarget(Vector3 towerPos, float range)
        {
            var enemies = _enemyService.GetAssumedActiveEnemies();
            if (enemies.Count <= 0)
                return null;

            var nearest = AttackTargeting.FindNearestEnemy(towerPos, enemies);
            return Vector3.Distance(towerPos, nearest.transform.position) <= range ? nearest : null;
        }
    }
}
