using System.Collections.Generic;
using Enums;
using Infrastructure.Impl;
using Services;
using Services.Impl;
using Systems.RunTime;
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
        private readonly IGameTimeProvider _gameTimeProvider;

        public SideTowerAttackSystem(
            SideTowerService sideTowerService,
            EnemyService enemyService,
            EntityFactory entityFactory,
            IGameTimeProvider gameTimeProvider
        )
        {
            _sideTowerService = sideTowerService;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Tick()
        {
            var deltaTime = _gameTimeProvider.DeltaTime;

            foreach (var tower in _sideTowerService.Towers)
            {
                switch (tower.attackType)
                {
                    case ESideTowerAttackType.Projectile:
                        Attack(tower, deltaTime);
                        break;
                    case ESideTowerAttackType.ChainLightning:
                        AttackChainLightning(tower, deltaTime);
                        break;
                    // Beam and SlowAura are handled by separate systems.
                }
            }
        }

        private void Attack(SideTowerView tower, float deltaTime)
        {
            if (tower.reloadRemaining > 0f)
            {
                tower.reloadRemaining -= deltaTime;
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
            // Без targetPoolVersion снаряд доп-башни на первом же кадре считался потерявшим цель
            // (BulletMovingSystem / BulletHitSystem сравнивают версию цели с этим полем), улетал
            // в никуда и никогда не попадал — а зарезервированный им урон навсегда оставался
            // висеть на живом враге, из-за чего главная башня переставала его видеть.
            bullet.targetPoolVersion = nearestEnemy.poolVersion;
            bullet.damage = tower.attackDamage;
            bullet.ReserveDamageOnTarget();

            tower.reloadRemaining = 1f / tower.attackSpeed;
        }

        private void AttackChainLightning(SideTowerView tower, float deltaTime)
        {
            if (tower.reloadRemaining > 0f)
            {
                tower.reloadRemaining -= deltaTime;

                if (tower.chainVisualRemaining > 0f)
                {
                    tower.chainVisualRemaining -= deltaTime;
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
                var next = AttackTargeting.FindNearestUnhit(previous.transform.position, enemies, hit, tower.chainJumpRadius);
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
        }

    }
}
