using System.Collections.Generic;
using Enums;
using Infrastructure.Impl;
using Services;
using Services.Impl;
using Systems.RunTime;
using Systems.RunTime.Bullets;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Tower
{
    public class TowerAttackSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly EntityFactory _entityFactory;
        private readonly TowerView _towerView;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly TowerBuffRuntimeService _towerBuffRuntimeService;

        private float _reloadRemaining;

        public TowerAttackSystem(
            TowerView towerView,
            EnemyService enemyService,
            EntityFactory entityFactory,
            IGameTimeProvider gameTimeProvider,
            TowerBuffRuntimeService towerBuffRuntimeService
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
            _gameTimeProvider = gameTimeProvider;
            _towerBuffRuntimeService = towerBuffRuntimeService;
        }

        public void Tick()
        {
            BulletImpactVfx.TickPierceLine(_towerView, _gameTimeProvider.DeltaTime);

            if (_reloadRemaining > 0f)
            {
                _reloadRemaining -= _gameTimeProvider.DeltaTime;
                return;
            }

            var enemies = _enemyService.GetAssumedActiveEnemies();
            if (enemies.Count <= 0)
                return;

            var towerPos = _towerView.transform.position;
            var nearestEnemy = AttackTargeting.FindNearestEnemy(towerPos, enemies);
            if (!CheckOnDistanceAttack(nearestEnemy.transform.position))
                return;

            var mainDirection = (nearestEnemy.transform.position - towerPos).normalized;
            var firedTargets = new List<EnemyView> { nearestEnemy };
            CreateConfiguredBullet(nearestEnemy, mainDirection);

            var stats = _towerBuffRuntimeService.Stats;
            var effectiveRange = GetEffectiveRange();

            for (var i = 0; i < stats.AdditionalForwardShots; i++)
            {
                var extraTarget = AttackTargeting.FindNearestUnhit(towerPos, enemies, firedTargets, effectiveRange);
                if (extraTarget == null)
                    break;

                firedTargets.Add(extraTarget);
                CreateConfiguredBullet(extraTarget, (extraTarget.transform.position - towerPos).normalized);
            }

            for (var i = 0; i < stats.BackShots; i++)
            {
                var backTarget = FindBackTarget(enemies, firedTargets, mainDirection, effectiveRange);
                if (backTarget == null)
                    break;

                firedTargets.Add(backTarget);
                CreateConfiguredBullet(backTarget, (backTarget.transform.position - towerPos).normalized);
            }

            _reloadRemaining = 1f / Mathf.Max(0.01f, _towerView.attackSpeed * stats.AttackSpeedMultiplier);
        }

        private bool CheckOnDistanceAttack(Vector3 enemyPos)
        {
            return Vector3.Distance(enemyPos, _towerView.transform.position) <= GetEffectiveRange();
        }

        private float GetEffectiveRange()
        {
            return _towerView.attackDistance
                   * _towerView.ratioRange
                   * _towerBuffRuntimeService.Stats.RangeMultiplier;
        }

        private int CalculateDamage()
        {
            var stats = _towerBuffRuntimeService.Stats;
            var damage = _towerView.attackDamage * stats.DamageMultiplier;

            if (stats.CritChance > 0f && Random.value < Mathf.Clamp01(stats.CritChance))
                damage *= Mathf.Max(1f, stats.CritDamageMultiplier);

            return Mathf.Max(1, Mathf.CeilToInt(damage));
        }

        private void CreateConfiguredBullet(EnemyView target, Vector3 launchDirection)
        {
            var bullet = (BulletView)_entityFactory.CreateBullet(_towerView.transform.position, _towerView.projectilePrefabVariant);
            bullet.target = target;
            bullet.damage = CalculateDamage();
            bullet.attackType = _towerView.attackType;
            bullet.speedMultiplier = _towerView.projectileSpeedMultiplier;
            bullet.launchPosition = _towerView.transform.position;
            bullet.launchDirection = launchDirection.sqrMagnitude > 0f ? launchDirection.normalized : _towerView.transform.forward;

            var stats = _towerBuffRuntimeService.Stats;
            bullet.ricochetRemaining = stats.RicochetCount;
            bullet.ricochetRadius = stats.RicochetRadius;
            bullet.ricochetFalloff = stats.RicochetFalloff;
            bullet.piercingLineRemaining = stats.PiercingLineCount;
            bullet.piercingLineWidth = stats.PiercingLineWidth;
            bullet.piercingLineFalloff = stats.PiercingLineFalloff;
            bullet.piercingLineRange = GetEffectiveRange();
            bullet.hitEnemies.Clear();
            bullet.hitEnemies.Add(target);

            bullet.projectileAppliesFrost = _towerView.projectileAppliesFrost;
            bullet.projectileFrostSlowPercent = _towerView.projectileFrostSlowPercent;
            bullet.projectileFrostSlowDuration = _towerView.projectileFrostSlowDuration;
            bullet.projectileAppliesPoison = _towerView.projectileAppliesPoison;
            bullet.projectilePoisonDamagePercentPerTick = _towerView.projectilePoisonDamagePercentPerTick;
            bullet.projectilePoisonTickInterval = _towerView.projectilePoisonTickInterval;
            bullet.projectilePoisonDuration = _towerView.projectilePoisonDuration;
            bullet.projectilePoisonVfxPrefab = _towerView.projectilePoisonVfxPrefab;

            switch (_towerView.attackType)
            {
                case EMainTowerAttackType.Splash:
                    bullet.splashRadius = _towerView.splashRadius;
                    bullet.splashFalloff = _towerView.splashFalloff;
                    bullet.splashImpactEffectPrefab = _towerView.splashImpactEffectPrefab;
                    bullet.splashImpactEffectReferenceRadius = _towerView.splashImpactEffectReferenceRadius;
                    break;
                case EMainTowerAttackType.Frost:
                    bullet.frostSlowPercent = _towerView.frostSlowPercent;
                    bullet.frostSlowDuration = _towerView.frostSlowDuration;
                    break;
                case EMainTowerAttackType.Pierce:
                    bullet.pierceCount = _towerView.pierceCount;
                    bullet.pierceJumpRadius = _towerView.pierceJumpRadius;
                    bullet.pierceFalloff = _towerView.pierceFalloff;
                    break;
            }

            target.healthComponent.ReduceAssumedHealth(bullet.damage);
        }

        private EnemyView FindBackTarget(
            IReadOnlyList<EnemyView> enemies,
            IReadOnlyList<EnemyView> excluded,
            Vector3 forwardDirection,
            float range)
        {
            EnemyView best = null;
            var bestDistance = float.MaxValue;
            var towerPos = _towerView.transform.position;

            foreach (var enemy in enemies)
            {
                if (Contains(excluded, enemy))
                    continue;

                var offset = enemy.transform.position - towerPos;
                var distance = offset.magnitude;
                if (distance > range || distance <= 0.01f)
                    continue;

                if (Vector3.Dot(forwardDirection, offset.normalized) > -0.35f)
                    continue;

                if (distance >= bestDistance)
                    continue;

                best = enemy;
                bestDistance = distance;
            }

            return best;
        }

        private static bool Contains(IReadOnlyList<EnemyView> enemies, EnemyView enemy)
        {
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == enemy)
                    return true;
            }

            return false;
        }
    }
}
