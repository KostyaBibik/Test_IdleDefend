using System.Collections.Generic;
using Enums;
using Services.Impl;
using Systems.RunTime;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Bullets
{
    public class BulletHitSystem : ITickable
    {
        private const float distanceCheckValue = 0.01f;

        private readonly BulletService _bulletService;
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;

        public BulletHitSystem(
            BulletService bulletService,
            EnemyService enemyService,
            TowerView towerView
        )
        {
            _bulletService = bulletService;
            _enemyService = enemyService;
            _towerView = towerView;
        }

        public void Tick()
        {
            foreach (var bullet in _bulletService.Bullets)
            {
                if (bullet.target == null)
                    continue;

                if (Vector3.Distance(bullet.target.transform.position, bullet.transform.position) > distanceCheckValue)
                    continue;

                HandleHit(bullet);
                return;
            }
        }

        private void HandleHit(BulletView bullet)
        {
            var primaryTarget = bullet.target;

            primaryTarget.healthComponent.ReduceHealth(bullet.damage);

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
            ApplyLinePierce(bullet, primaryTarget);

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
                next.healthComponent.ReduceHealth(damageInt);

                hit.Add(next);
                previous = next;
            }

            var hitPoints = new List<Vector3>(hit.Count);
            foreach (var enemy in hit)
                hitPoints.Add(enemy.transform.position);

            BulletImpactVfx.ShowPierceLine(_towerView, towerPos, hitPoints);
        }

        private void ApplyLinePierce(BulletView bullet, EnemyView primaryTarget)
        {
            if (bullet.piercingLineRemaining <= 0)
                return;

            var direction = bullet.launchDirection.sqrMagnitude > 0f
                ? bullet.launchDirection.normalized
                : (primaryTarget.transform.position - bullet.launchPosition).normalized;

            if (direction.sqrMagnitude <= 0f)
                return;

            var origin = primaryTarget.transform.position;
            var maxDistance = Mathf.Max(0f, bullet.piercingLineRange - Vector3.Distance(bullet.launchPosition, origin));
            if (maxDistance <= 0f)
                return;

            var candidates = new List<LinePierceCandidate>();
            foreach (var enemy in _enemyService.GetAssumedActiveEnemies())
            {
                if (enemy == primaryTarget || ContainsHit(bullet, enemy))
                    continue;

                var offset = enemy.transform.position - origin;
                var projection = Vector3.Dot(offset, direction);
                if (projection <= 0f || projection > maxDistance)
                    continue;

                var closestPoint = origin + direction * projection;
                var sideDistance = Vector3.Distance(enemy.transform.position, closestPoint);
                if (sideDistance > bullet.piercingLineWidth)
                    continue;

                candidates.Add(new LinePierceCandidate(enemy, projection));
            }

            candidates.Sort((a, b) => a.Projection.CompareTo(b.Projection));

            var damage = (float)bullet.damage;
            var hitPoints = new List<Vector3> { primaryTarget.transform.position };
            var count = Mathf.Min(bullet.piercingLineRemaining, candidates.Count);
            for (var i = 0; i < count; i++)
            {
                damage *= bullet.piercingLineFalloff;
                var enemy = candidates[i].Enemy;
                enemy.healthComponent.ReduceHealth(Mathf.Max(1, Mathf.RoundToInt(damage)));
                bullet.hitEnemies.Add(enemy);
                hitPoints.Add(enemy.transform.position);
            }

            if (hitPoints.Count > 1)
                BulletImpactVfx.ShowPierceLine(_towerView, bullet.launchPosition, hitPoints);
        }

        private bool TryStartRicochet(BulletView bullet, EnemyView previousTarget)
        {
            if (bullet.ricochetRemaining <= 0)
                return false;

            var nextTarget = AttackTargeting.FindNearestUnhit(
                previousTarget.transform.position,
                _enemyService.GetAssumedActiveEnemies(),
                bullet.hitEnemies,
                bullet.ricochetRadius);

            if (nextTarget == null)
                return false;

            bullet.ricochetRemaining--;
            bullet.damage = Mathf.Max(1, Mathf.RoundToInt(bullet.damage * bullet.ricochetFalloff));
            bullet.target = nextTarget;
            bullet.hitEnemies.Add(nextTarget);
            nextTarget.healthComponent.ReduceAssumedHealth(bullet.damage);
            return true;
        }

        private void ApplySplash(BulletView bullet)
        {
            var center = bullet.target.transform.position;
            var damage = (float)bullet.damage * bullet.splashFalloff;
            var damageInt = Mathf.RoundToInt(damage);

            foreach (var enemy in _enemyService.Enemies)
            {
                if (enemy == bullet.target)
                    continue;

                if (Vector3.Distance(center, enemy.transform.position) > bullet.splashRadius)
                    continue;

                enemy.healthComponent.ReduceHealth(damageInt);
            }

            BulletImpactVfx.SpawnSplashImpact(
                bullet.splashImpactEffectPrefab,
                center,
                bullet.splashRadius,
                bullet.splashImpactEffectReferenceRadius);
        }

        private static bool ContainsHit(BulletView bullet, EnemyView enemy)
        {
            for (var i = 0; i < bullet.hitEnemies.Count; i++)
            {
                if (bullet.hitEnemies[i] == enemy)
                    return true;
            }

            return false;
        }

        private readonly struct LinePierceCandidate
        {
            public LinePierceCandidate(EnemyView enemy, float projection)
            {
                Enemy = enemy;
                Projection = projection;
            }

            public EnemyView Enemy { get; }
            public float Projection { get; }
        }
    }
}
