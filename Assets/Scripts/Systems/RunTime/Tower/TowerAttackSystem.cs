using System.Collections.Generic;
using Enums;
using Infrastructure.Impl;
using Services;
using Services.Impl;
using Signals;
using Systems.RunTime;
using Systems.RunTime.Bullets;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Tower
{
    public class TowerAttackSystem : IInitializable, ITickable, System.IDisposable
    {
        private readonly EnemyService _enemyService;
        private readonly EntityFactory _entityFactory;
        private readonly TowerView _towerView;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly TowerBuffRuntimeService _towerBuffRuntimeService;
        private readonly SignalBus _signalBus;

        private float _reloadRemaining;
        private float _overloadRemaining;

        public TowerAttackSystem(
            TowerView towerView,
            EnemyService enemyService,
            EntityFactory entityFactory,
            IGameTimeProvider gameTimeProvider,
            TowerBuffRuntimeService towerBuffRuntimeService,
            SignalBus signalBus
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
            _gameTimeProvider = gameTimeProvider;
            _towerBuffRuntimeService = towerBuffRuntimeService;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<DestroyEntitySignal>(OnEnemyDestroyed);
        }

        public void Tick()
        {
            BulletImpactVfx.TickPierceLine(_towerView, _gameTimeProvider.DeltaTime);

            if (_overloadRemaining > 0f)
                _overloadRemaining = Mathf.Max(0f, _overloadRemaining - _gameTimeProvider.DeltaTime);

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
            var shotPlan = new List<ShotRequest>
            {
                new ShotRequest(nearestEnemy, mainDirection)
            };

            var stats = _towerBuffRuntimeService.Stats;
            var effectiveRange = GetEffectiveRange();

            for (var i = 0; i < stats.AdditionalForwardShots; i++)
            {
                var extraTarget = AttackTargeting.FindNearestUnhit(towerPos, enemies, firedTargets, effectiveRange);
                if (extraTarget == null)
                    break;

                firedTargets.Add(extraTarget);
                shotPlan.Add(new ShotRequest(extraTarget, (extraTarget.transform.position - towerPos).normalized));
            }

            for (var i = 0; i < stats.BackShots; i++)
            {
                var backTarget = FindBackTarget(enemies, firedTargets, mainDirection, effectiveRange);
                if (backTarget == null)
                    break;

                firedTargets.Add(backTarget);
                shotPlan.Add(new ShotRequest(backTarget, (backTarget.transform.position - towerPos).normalized));
            }

            FireShotPlan(shotPlan, Mathf.Max(1, 1 + stats.MultishotRepeats));
            _reloadRemaining = 1f / Mathf.Max(0.01f, _towerView.attackSpeed * GetAttackSpeedMultiplier());
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

        private float GetAttackSpeedMultiplier()
        {
            var stats = _towerBuffRuntimeService.Stats;
            var multiplier = stats.AttackSpeedMultiplier;

            if (_overloadRemaining > 0f)
                multiplier *= Mathf.Max(1f, stats.OverloadAttackSpeedMultiplier);

            return multiplier;
        }

        private DamageRoll CalculateDamage()
        {
            var stats = _towerBuffRuntimeService.Stats;
            var damage = _towerView.attackDamage * stats.DamageMultiplier;
            var isCritical = false;

            if (stats.CritChance > 0f && Random.value < Mathf.Clamp01(stats.CritChance))
            {
                damage *= Mathf.Max(1f, stats.CritDamageMultiplier);
                isCritical = true;
            }

            return new DamageRoll(Mathf.Max(1, Mathf.CeilToInt(damage)), isCritical);
        }

        private void CreateConfiguredBullet(EnemyView target, Vector3 launchDirection)
        {
            var bullet = (BulletView)_entityFactory.CreateBullet(_towerView.transform.position, _towerView.projectilePrefabVariant);
            var damageRoll = CalculateDamage();

            bullet.target = target;
            bullet.damage = damageRoll.Damage;
            bullet.isCritical = damageRoll.IsCritical;
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
            bullet.explosiveShotRadius = stats.ExplosiveShotRadius;
            bullet.explosiveShotFalloff = stats.ExplosiveShotFalloff;
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

        private void FireShotPlan(IReadOnlyList<ShotRequest> shotPlan, int volleys)
        {
            for (var volley = 0; volley < volleys; volley++)
            {
                for (var i = 0; i < shotPlan.Count; i++)
                {
                    var shot = shotPlan[i];
                    if (shot.Target == null || shot.Target.isDestroyed)
                        continue;

                    CreateConfiguredBullet(shot.Target, shot.Direction);
                }
            }
        }

        private void OnEnemyDestroyed(DestroyEntitySignal signal)
        {
            if (!signal.hashReward)
                return;

            var stats = _towerBuffRuntimeService.Stats;
            if (stats.OverloadDuration <= 0f || stats.OverloadAttackSpeedMultiplier <= 1f)
                return;

            _overloadRemaining = stats.OverloadDuration;
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

        private readonly struct DamageRoll
        {
            public DamageRoll(int damage, bool isCritical)
            {
                Damage = damage;
                IsCritical = isCritical;
            }

            public int Damage { get; }
            public bool IsCritical { get; }
        }

        private readonly struct ShotRequest
        {
            public ShotRequest(EnemyView target, Vector3 direction)
            {
                Target = target;
                Direction = direction;
            }

            public EnemyView Target { get; }
            public Vector3 Direction { get; }
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<DestroyEntitySignal>(OnEnemyDestroyed);
        }
    }
}
