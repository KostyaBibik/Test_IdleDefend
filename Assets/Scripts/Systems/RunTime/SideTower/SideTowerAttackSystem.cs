using System.Collections.Generic;
using Enums;
using Infrastructure.Impl;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.SideTower
{
    public class SideTowerAttackSystem : ITickable
    {
        private readonly SideTowerService _sideTowerService;
        private readonly EnemyService _enemyService;
        private readonly EntityFactory _entityFactory;

        public SideTowerAttackSystem(
            SideTowerService sideTowerService,
            EnemyService enemyService,
            EntityFactory entityFactory
        )
        {
            _sideTowerService = sideTowerService;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
        }

        public void Tick()
        {
            foreach (var tower in _sideTowerService.Towers)
            {
                switch (tower.attackType)
                {
                    case ESideTowerAttackType.Projectile:
                        Attack(tower);
                        break;
                    case ESideTowerAttackType.ChainLightning:
                        AttackChainLightning(tower);
                        break;
                    // Beam и SlowAura обслуживаются отдельными системами (SideTowerBeamSystem, SideTowerSlowAuraSystem).
                }
            }
        }

        private void Attack(SideTowerView tower)
        {
            if (tower.reloadRemaining > 0f)
            {
                tower.reloadRemaining -= Time.deltaTime;
                return;
            }

            var enemies = _enemyService.GetAssumedActiveEnemies();
            if (enemies.Count <= 0)
                return;

            var towerPos = tower.transform.position;
            var nearestEnemy = AttackTargeting.FindNearestEnemy(towerPos, enemies);

            if (Vector3.Distance(towerPos, nearestEnemy.transform.position) > tower.attackDistance)
                return;

            var bullet = (BulletView) _entityFactory.CreateBullet(towerPos);
            bullet.target = nearestEnemy;
            bullet.damage = tower.attackDamage;
            nearestEnemy.healthComponent.ReduceAssumedHealth(bullet.damage);

            tower.reloadRemaining = 1f / tower.attackSpeed;
        }

        private void AttackChainLightning(SideTowerView tower)
        {
            if (tower.reloadRemaining > 0f)
            {
                tower.reloadRemaining -= Time.deltaTime;

                if (tower.chainVisualRemaining > 0f)
                {
                    tower.chainVisualRemaining -= Time.deltaTime;
                    if (tower.chainVisualRemaining <= 0f && tower.ChainLine != null)
                        tower.ChainLine.positionCount = 0;
                }

                return;
            }

            var enemies = _enemyService.GetAssumedActiveEnemies();
            if (enemies.Count <= 0)
                return;

            var towerPos = tower.transform.position;
            var firstTarget = AttackTargeting.FindNearestEnemy(towerPos, enemies);

            if (Vector3.Distance(towerPos, firstTarget.transform.position) > tower.attackDistance)
                return;

            var hit = new List<EnemyView> { firstTarget };
            var damage = (float) tower.attackDamage;
            ApplyChainDamage(firstTarget, damage);

            var previous = firstTarget;
            for (var i = 0; i < tower.chainJumpCount; i++)
            {
                damage *= tower.chainFalloffFactor;
                var next = FindNearestUnhit(previous.transform.position, enemies, hit, tower.chainJumpRadius);
                if (next == null)
                    break;

                ApplyChainDamage(next, damage);
                hit.Add(next);
                previous = next;
            }

            if (tower.ChainLine != null)
            {
                if (tower.ChainLineMaterials != null && tower.ChainLineMaterials.Length > 0)
                    tower.ChainLine.material = tower.ChainLineMaterials[Random.Range(0, tower.ChainLineMaterials.Length)];

                tower.ChainLine.positionCount = hit.Count + 1;
                tower.ChainLine.SetPosition(0, towerPos);
                for (var i = 0; i < hit.Count; i++)
                    tower.ChainLine.SetPosition(i + 1, hit[i].transform.position);
            }
            tower.chainVisualRemaining = tower.chainVisualDuration;

            tower.reloadRemaining = 1f / tower.attackSpeed;
        }

        private static void ApplyChainDamage(EnemyView target, float damage)
        {
            var damageInt = Mathf.RoundToInt(damage);
            target.healthComponent.ReduceHealth(damageInt);
            target.healthComponent.ReduceAssumedHealth(damageInt);
        }

        private static EnemyView FindNearestUnhit(Vector3 fromPosition, IList<EnemyView> allEnemies, List<EnemyView> alreadyHit, float radius)
        {
            EnemyView nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var candidate in allEnemies)
            {
                if (alreadyHit.Contains(candidate))
                    continue;

                var distance = Vector3.Distance(fromPosition, candidate.transform.position);
                if (distance > radius)
                    continue;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }

            return nearest;
        }
    }
}
