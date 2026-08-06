using System.Collections.Generic;
using Enums;
using Services.Impl;
using Signals;
using Systems.RunTime;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Bullets
{
    public class BulletHitSystem : ITickable
    {
        private const float distanceCheckValue = 0.01f;
        private const float splashDamagePercent = 0.3f;

        private readonly BulletService _bulletService;
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;
        private readonly SignalBus _signalBus;

        public BulletHitSystem(
            BulletService bulletService,
            EnemyService enemyService,
            TowerView towerView,
            SignalBus signalBus
        )
        {
            _bulletService = bulletService;
            _enemyService = enemyService;
            _towerView = towerView;
            _signalBus = signalBus;
        }

        public void Tick()
        {
            // Обход с конца по индексу, а не foreach: попадание может удалить снаряд из списка
            // (BulletService.RemoveEntityFromService), а это ломает foreach. Раньше от этого
            // спасались выходом из метода после первого же попадания — то есть за кадр
            // засчитывался ровно один удар на всю сцену. При двух-трёх стволах и мультишоте
            // снаряды копились быстрее, чем разрешались, попадания уезжали на секунды, и враги
            // успевали дойти до башни сквозь висящие в них снаряды.
            var bullets = _bulletService.Bullets;
            for (var i = bullets.Count - 1; i >= 0; i--)
            {
                if (i >= bullets.Count)
                    continue;

                var bullet = bullets[i];
                if (bullet == null)
                    continue;

                if (bullet.target == null || bullet.target.isDestroyed
                    || bullet.target.poolVersion != bullet.targetPoolVersion)
                {
                    TryHandleFreeFlightHit(bullet);
                    continue;
                }

                if (Vector3.Distance(bullet.target.transform.position, bullet.transform.position) > distanceCheckValue)
                    continue;

                HandleHit(bullet);
            }
        }

        private bool TryHandleFreeFlightHit(BulletView bullet)
        {
            if (!bullet.continueOnTargetLost)
                return false;

            var target = FindFreeFlightTarget(bullet);
            if (target == null)
                return false;

            bullet.target = target;
            bullet.targetPoolVersion = target.poolVersion;
            bullet.hitEnemies.Add(new BulletView.HitRecord(target, target.poolVersion));
            bullet.ReserveDamageOnTarget();
            HandleHit(bullet);
            return true;
        }

        private EnemyView FindFreeFlightTarget(BulletView bullet)
        {
            var start = bullet.previousPosition;
            var end = bullet.transform.position;
            var radius = Mathf.Max(0.01f, bullet.freeFlightCollisionRadius);
            var bestProjection = float.MaxValue;
            EnemyView best = null;

            foreach (var enemy in _enemyService.GetAssumedActiveEnemies())
            {
                if (enemy == null || enemy.isDestroyed || ContainsHit(bullet, enemy))
                    continue;

                if (!TryGetSegmentProjection(start, end, enemy.transform.position, out var projection, out var distance))
                    continue;

                if (distance > radius || projection >= bestProjection)
                    continue;

                best = enemy;
                bestProjection = projection;
            }

            return best;
        }

        private static bool TryGetSegmentProjection(
            Vector3 start,
            Vector3 end,
            Vector3 point,
            out float projection,
            out float distance)
        {
            var segment = end - start;
            var segmentLengthSqr = segment.sqrMagnitude;
            if (segmentLengthSqr <= 0.0001f)
            {
                projection = 0f;
                distance = Vector3.Distance(point, end);
                return true;
            }

            var t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / segmentLengthSqr);
            var closestPoint = start + segment * t;
            projection = t;
            distance = Vector3.Distance(point, closestPoint);
            return true;
        }

        private void HandleHit(BulletView bullet)
        {
            var primaryTarget = bullet.target;

            ApplyDamage(bullet, primaryTarget, bullet.damage, true);

            // Резерв по основной цели отработан: ApplyDamage уже снял его внутри ReduceHealth.
            // Обнуляем счётчик на снаряде, иначе рикошет/продолжение полёта зарезервируют поверх,
            // а BulletService при удалении вернул бы врагу урон, который тот уже получил.
            bullet.reservedDamage = 0;

            switch (bullet.attackType)
            {
                case EMainTowerAttackType.Splash:
                    ApplySplash(bullet);
                    break;
                case EMainTowerAttackType.Frost:
                    ApplyFrost(primaryTarget, bullet.frostSlowPercent, bullet.frostSlowDuration);
                    break;
                case EMainTowerAttackType.Pierce:
                    ApplyChainPierce(bullet);
                    break;
            }

            ApplyProjectileEffects(bullet, primaryTarget);
            ApplyExplosiveShot(bullet, primaryTarget);

            if (TryContinuePiercingShot(bullet, primaryTarget))
                return;

            if (TryStartRicochet(bullet, primaryTarget))
                return;

            _bulletService.RemoveEntityFromService(bullet);
        }

        private static void ApplyFrost(EnemyView target, float slowPercent, float duration)
        {
            if (target == null || target.isDestroyed)
                return;

            target.ApplyFrost(Mathf.Clamp01(1f - slowPercent), duration);
        }

        private static void ApplyProjectileEffects(BulletView bullet, EnemyView target)
        {
            if (target == null || target.isDestroyed)
                return;

            if (bullet.projectileAppliesFrost)
            {
                target.ApplyFrost(
                    Mathf.Clamp01(1f - bullet.projectileFrostSlowPercent),
                    bullet.projectileFrostSlowDuration);
            }

            if (bullet.projectileAppliesPoison)
            {
                target.ApplyPoison(
                    bullet.damage * bullet.projectilePoisonDamagePercentPerTick,
                    bullet.projectilePoisonTickInterval,
                    bullet.projectilePoisonDuration,
                    bullet.projectilePoisonVfxPrefab);
            }
        }

        private void ApplyChainPierce(BulletView bullet)
        {
            var towerPos = _towerView.transform.position;
            var enemies = _enemyService.GetAssumedActiveEnemies();
            var hit = new List<EnemyView> { bullet.target };
            var damage = (float)bullet.damage;

            var previous = bullet.target;
            for (var i = 0; i < bullet.pierceCount - 1; i++)
            {
                damage *= bullet.pierceFalloff;
                var next = AttackTargeting.FindNearestUnhit(previous.transform.position, enemies, hit, bullet.pierceJumpRadius);
                if (next == null)
                    break;

                var damageInt = Mathf.RoundToInt(damage);
                ApplyDamage(bullet, next, damageInt, false);

                hit.Add(next);
                previous = next;
            }

            var hitPoints = new List<Vector3>(hit.Count);
            foreach (var enemy in hit)
                hitPoints.Add(enemy.transform.position);

            BulletImpactVfx.ShowPierceLine(_towerView, towerPos, hitPoints);
        }

        private bool TryContinuePiercingShot(BulletView bullet, EnemyView primaryTarget)
        {
            if (bullet.piercingLineRemaining <= 0)
                return false;

            var direction = bullet.launchDirection.sqrMagnitude > 0f
                ? bullet.launchDirection.normalized
                : (primaryTarget.transform.position - bullet.launchPosition).normalized;

            if (direction.sqrMagnitude <= 0f)
                return false;

            var origin = primaryTarget.transform.position;
            var maxDistance = Mathf.Max(0f, bullet.piercingLineRange - Vector3.Distance(bullet.launchPosition, origin));
            if (maxDistance <= 0f)
                return false;

            bullet.piercingLineRemaining--;
            bullet.target = null;
            bullet.targetPoolVersion = 0;
            bullet.continueOnTargetLost = true;
            bullet.freeFlightDirection = direction;
            bullet.freeFlightRemainingDistance = maxDistance;
            bullet.freeFlightRemainingSeconds = 0f;
            bullet.freeFlightCollisionRadius = Mathf.Max(bullet.freeFlightCollisionRadius, bullet.piercingLineWidth);
            bullet.previousPosition = bullet.transform.position;

            return true;
        }

        private void ApplyExplosiveShot(BulletView bullet, EnemyView primaryTarget)
        {
            if (bullet.explosiveShotRadius <= 0f)
                return;

            var center = primaryTarget.transform.position;
            var damage = Mathf.Max(1, Mathf.RoundToInt(bullet.damage * bullet.explosiveShotFalloff));

            var enemies = _enemyService.GetAssumedActiveEnemies();
            foreach (var enemy in enemies)
            {
                if (enemy == primaryTarget || ContainsHit(bullet, enemy))
                    continue;

                if (Vector3.Distance(center, enemy.transform.position) > bullet.explosiveShotRadius)
                    continue;

                ApplyDamage(bullet, enemy, damage, false);
            }
        }

        private bool TryStartRicochet(BulletView bullet, EnemyView previousTarget)
        {
            if (bullet.ricochetRemaining <= 0)
                return false;

            var nextTarget = AttackTargeting.FindNearestUnhit(
                previousTarget.transform.position,
                _enemyService.GetAssumedActiveEnemies(),
                GetStillValidHitViews(bullet),
                bullet.ricochetRadius);

            if (nextTarget == null)
                return false;

            bullet.ricochetRemaining--;
            bullet.damage = Mathf.Max(1, Mathf.RoundToInt(bullet.damage * bullet.ricochetFalloff));
            bullet.target = nextTarget;
            bullet.targetPoolVersion = nextTarget.poolVersion;
            bullet.hitEnemies.Add(new BulletView.HitRecord(nextTarget, nextTarget.poolVersion));
            bullet.ReserveDamageOnTarget();
            return true;
        }

        private void ApplySplash(BulletView bullet)
        {
            var center = bullet.target.transform.position;
            var damageInt = Mathf.Max(1, Mathf.CeilToInt(bullet.damage * splashDamagePercent));

            var enemies = _enemyService.GetAssumedActiveEnemies();
            foreach (var enemy in enemies)
            {
                if (enemy == bullet.target)
                    continue;

                if (Vector3.Distance(center, enemy.transform.position) > bullet.splashRadius)
                    continue;

                ApplyDamage(bullet, enemy, damageInt, false);
            }

            BulletImpactVfx.SpawnSplashImpact(
                bullet.splashImpactEffectPrefab,
                center,
                bullet.splashRadius,
                bullet.splashImpactEffectReferenceRadius);
        }

        /// <summary>
        /// Сравнивает и ссылку, и poolVersion (см. IEntityView.poolVersion): если запись в
        /// hitEnemies осталась от врага, чей GameObject уже вернулся в пул и переиспользован под
        /// другого (нового) врага, она больше не должна засчитываться как "уже подбит этим снарядом" -
        /// иначе новый враг ошибочно считался бы неуязвимым/пропущенным для этого снаряда.
        /// </summary>
        private static bool ContainsHit(BulletView bullet, EnemyView enemy)
        {
            for (var i = 0; i < bullet.hitEnemies.Count; i++)
            {
                var record = bullet.hitEnemies[i];
                if (record.View == enemy && record.Version == enemy.poolVersion)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Тот же принцип, что и в ContainsHit, но в форме списка - нужен там, где общий
        /// AttackTargeting.FindNearestUnhit ожидает ICollection&lt;EnemyView&gt; (эту сигнатуру
        /// он ещё делит с SideTowerAttackSystem, поэтому подгонять её под HitRecord не стоит).
        /// </summary>
        private static List<EnemyView> GetStillValidHitViews(BulletView bullet)
        {
            var result = new List<EnemyView>(bullet.hitEnemies.Count);
            foreach (var record in bullet.hitEnemies)
            {
                if (record.View != null && record.View.poolVersion == record.Version)
                    result.Add(record.View);
            }

            return result;
        }

        private void ApplyDamage(BulletView bullet, EnemyView target, int rawDamage, bool isPrimaryHit)
        {
            if (target == null || target.isDestroyed)
                return;

            var effectiveDamage = target.healthComponent.GetEffectiveDamage(rawDamage);
            target.healthComponent.ReduceHealth(rawDamage);

            _signalBus.Fire(new TowerDamageDealtSignal
            {
                target = target,
                worldPos = target.transform.position,
                damage = Mathf.Max(0, effectiveDamage),
                isCritical = bullet.isCritical,
                isPrimaryHit = isPrimaryHit
            });
        }

    }
}
