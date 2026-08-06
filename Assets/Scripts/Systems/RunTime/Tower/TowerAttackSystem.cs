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
        private const float parallelShotSpacing = 0.14f;
        private const float backShotLifetimeSeconds = 3f;

        private readonly EnemyService _enemyService;
        private readonly EntityFactory _entityFactory;
        private readonly TowerView _towerView;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly TowerBuffRuntimeService _towerBuffRuntimeService;
        private readonly ActiveBoostService _activeBoostService;
        private readonly SignalBus _signalBus;

        private float _reloadRemaining;
        private float _overloadRemaining;

        public TowerAttackSystem(
            TowerView towerView,
            EnemyService enemyService,
            EntityFactory entityFactory,
            IGameTimeProvider gameTimeProvider,
            TowerBuffRuntimeService towerBuffRuntimeService,
            ActiveBoostService activeBoostService,
            SignalBus signalBus
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
            _gameTimeProvider = gameTimeProvider;
            _towerBuffRuntimeService = towerBuffRuntimeService;
            _activeBoostService = activeBoostService;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            // Множители бустов теперь читаются каждый выстрел, а не вшиваются в статы один раз,
            // поэтому сервис должен быть прогружен независимо от порядка биндингов в GameInstaller.
            _activeBoostService.EnsureLoaded();
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
            var shotPlan = new List<ShotRequest>
            {
                new ShotRequest(nearestEnemy, mainDirection, 0f)
            };

            var stats = _towerBuffRuntimeService.Stats;
            AddParallelForwardShots(shotPlan, nearestEnemy, mainDirection, stats.ParallelForwardShots);

            for (var i = 0; i < stats.BackShots; i++)
                shotPlan.Add(new ShotRequest(null, -mainDirection, GetBackShotLateralOffset(i, stats.BackShots)));

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
                   * _towerBuffRuntimeService.Stats.RangeMultiplier
                   * _activeBoostService.RangeMultiplier;
        }

        private float GetAttackSpeedMultiplier()
        {
            var stats = _towerBuffRuntimeService.Stats;
            var multiplier = stats.AttackSpeedMultiplier
                             * _activeBoostService.AttackSpeedMultiplier
                             * Mathf.Max(1f, _towerView.ultimateAttackSpeedMultiplier);

            if (_overloadRemaining > 0f)
                multiplier *= Mathf.Max(1f, stats.OverloadAttackSpeedMultiplier);

            return multiplier;
        }

        private DamageRoll CalculateDamage()
        {
            var stats = _towerBuffRuntimeService.Stats;
            var damage = _towerView.attackDamage * stats.DamageMultiplier * _activeBoostService.DamageMultiplier;
            var isCritical = false;

            if (stats.CritChance > 0f && Random.value < Mathf.Clamp01(stats.CritChance))
            {
                damage *= Mathf.Max(1f, stats.CritDamageMultiplier);
                isCritical = true;
            }

            return new DamageRoll(Mathf.Max(1, Mathf.CeilToInt(damage)), isCritical);
        }

        private void AddParallelForwardShots(
            List<ShotRequest> shotPlan,
            EnemyView target,
            Vector3 direction,
            int additionalShots)
        {
            if (additionalShots <= 0)
                return;

            var totalShots = 1 + additionalShots;
            for (var i = 0; i < totalShots; i++)
            {
                var laneIndex = i - (totalShots - 1) * 0.5f;
                var request = new ShotRequest(target, direction, laneIndex * parallelShotSpacing);
                if (i == 0)
                    shotPlan[0] = request;
                else
                    shotPlan.Add(request);
            }
        }

        private void CreateConfiguredBullet(EnemyView target, Vector3 launchDirection, float lateralOffset)
        {
            var spawnPosition = _towerView.transform.position + GetLateralOffset(launchDirection, lateralOffset);
            var bullet = (BulletView)_entityFactory.CreateBullet(spawnPosition, _towerView.projectilePrefabVariant);
            var damageRoll = CalculateDamage();

            bullet.target = target;
            bullet.targetPoolVersion = target != null ? target.poolVersion : 0;
            bullet.damage = damageRoll.Damage;
            bullet.isCritical = damageRoll.IsCritical;
            bullet.attackType = _towerView.attackType;
            bullet.speedMultiplier = _towerView.projectileSpeedMultiplier;
            bullet.launchPosition = spawnPosition;
            bullet.launchDirection = launchDirection.sqrMagnitude > 0f ? launchDirection.normalized : _towerView.transform.forward;
            bullet.previousPosition = spawnPosition;
            bullet.freeFlightDirection = bullet.launchDirection;
            bullet.freeFlightRemainingDistance = target == null ? 0f : GetEffectiveRange();
            bullet.freeFlightRemainingSeconds = target == null ? backShotLifetimeSeconds : 0f;
            bullet.continueOnTargetLost = true;

            var stats = _towerBuffRuntimeService.Stats;
            bullet.ricochetRemaining = stats.RicochetCount;
            bullet.ricochetRadius = stats.RicochetRadius;
            bullet.ricochetFalloff = stats.RicochetFalloff;
            bullet.piercingLineRemaining = stats.PiercingLineCount > 0 ? 1 : 0;
            bullet.piercingLineWidth = stats.PiercingLineWidth;
            bullet.piercingLineFalloff = stats.PiercingLineFalloff;
            bullet.piercingLineRange = GetEffectiveRange();
            bullet.explosiveShotRadius = stats.ExplosiveShotRadius;
            bullet.explosiveShotFalloff = stats.ExplosiveShotFalloff;
            bullet.hitEnemies.Clear();
            if (target != null)
                bullet.hitEnemies.Add(new BulletView.HitRecord(target, target.poolVersion));

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

            if (target != null)
                target.healthComponent.ReduceAssumedHealth(bullet.damage);
        }

        private static float GetBackShotLateralOffset(int index, int count)
        {
            if (count <= 1)
                return 0f;

            return (index - (count - 1) * 0.5f) * parallelShotSpacing;
        }

        private static Vector3 GetLateralOffset(Vector3 direction, float offset)
        {
            if (Mathf.Approximately(offset, 0f))
                return Vector3.zero;

            var normalized = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
            var right = Vector3.Cross(Vector3.forward, normalized).normalized;
            return right * offset;
        }

        private void FireShotPlan(IReadOnlyList<ShotRequest> shotPlan, int volleys)
        {
            for (var volley = 0; volley < volleys; volley++)
            {
                for (var i = 0; i < shotPlan.Count; i++)
                {
                    var shot = shotPlan[i];
                    if (shot.Target != null && shot.Target.isDestroyed)
                        continue;

                    CreateConfiguredBullet(shot.Target, shot.Direction, shot.LateralOffset);
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
            public ShotRequest(EnemyView target, Vector3 direction, float lateralOffset)
            {
                Target = target;
                Direction = direction;
                LateralOffset = lateralOffset;
            }

            public EnemyView Target { get; }
            public Vector3 Direction { get; }
            public float LateralOffset { get; }
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<DestroyEntitySignal>(OnEnemyDestroyed);
        }
    }
}
