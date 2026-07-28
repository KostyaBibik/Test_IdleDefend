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


                if (Vector3.Distance(bullet.target.transform.position, bullet.transform.position) <= distanceCheckValue)
                {
                    _bulletService.RemoveEntityFromService(bullet);
                    bullet.target.healthComponent.ReduceHealth(bullet.damage);

                    switch (bullet.attackType)
                    {
                        case EMainTowerAttackType.Splash:
                            ApplySplash(bullet);
                            break;
                        case EMainTowerAttackType.Frost:
                            ApplyFrost(bullet);
                            break;
                        case EMainTowerAttackType.Pierce:
                            ApplyPierce(bullet);
                            break;
                    }

                    return;
                }
            }
        }

        private static void ApplyFrost(BulletView bullet)
        {
            bullet.target.ApplyFrost(Mathf.Clamp01(1f - bullet.frostSlowPercent), bullet.frostSlowDuration);
        }

        private void ApplyPierce(BulletView bullet)
        {
            var towerPos = _towerView.transform.position;
            var enemies = _enemyService.GetAssumedActiveEnemies();
            var hit = new List<EnemyView> { bullet.target };
            var damage = (float) bullet.damage;

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

        private void ApplySplash(BulletView bullet)
        {
            var center = bullet.target.transform.position;
            var damage = (float) bullet.damage * bullet.splashFalloff;
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
    }
}
