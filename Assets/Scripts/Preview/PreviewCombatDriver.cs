using System.Collections.Generic;
using Db;
using Enums;
using Systems.RunTime.Bullets;
using UnityEngine;
using Views.Impl;
using Random = UnityEngine.Random;

namespace Preview
{
    /// <summary>
    /// Имитация боя на витрине: манекены подлетают к башне с краёв кадра, башня стреляет теми же
    /// снарядами и рисует те же эффекты попадания, что и в бою (через BulletImpactVfx).
    ///
    /// Сознательно не переиспользует боевые системы: те завязаны на Zenject, EnemyService и
    /// LevelService, и тащить их в меню значило бы держать половину боя живой ради картинки.
    /// Граница простая: витрина читает только Db-дефиниции и общий визуальный хелпер, и никогда -
    /// игровые сервисы. Правки баланса её не касаются, правки визуала подхватываются сами.
    /// </summary>
    [DisallowMultipleComponent]
    public class PreviewCombatDriver : MonoBehaviour
    {
        private readonly List<PreviewEnemy> _enemies = new();
        private readonly List<PreviewBullet> _bullets = new();
        private readonly List<Vector3> _pierceHitPoints = new();

        private TowerPreviewStage _stage;
        private TowerView _towerView;
        private TowerPreviewSettings _settings;

        private float _spawnRemaining;
        private float _reloadRemaining;
        private bool _running;

        /// <summary>Есть ли кто-то в кадре - по этому признаку витрина отъезжает с крупного плана.</summary>
        public bool HasEnemies => _enemies.Count > 0;

        public void Begin(TowerPreviewStage stage, TowerView towerView)
        {
            _stage = stage;
            _towerView = towerView;
            _settings = stage.Settings;

            if (_settings == null || _settings.EnemyDefinition == null || _settings.EnemyDefinition.ViewPrefab == null)
            {
                // Башню всё равно показываем - просто без имитации боя.
                _running = false;
                return;
            }

            _spawnRemaining = _settings.FirstSpawnDelay;
            _reloadRemaining = 0f;
            _running = true;
        }

        public void StopAndCleanup()
        {
            _running = false;

            foreach (var enemy in _enemies)
            {
                if (enemy.View != null)
                    Destroy(enemy.View.gameObject);
            }

            foreach (var bullet in _bullets)
            {
                if (bullet.View != null)
                    Destroy(bullet.View.gameObject);
            }

            _enemies.Clear();
            _bullets.Clear();
        }

        private void Update()
        {
            if (!_running || _towerView == null)
                return;

            // Витрина живёт в меню и не должна зависеть от Time.timeScale (пауза, ускорения).
            var deltaTime = Time.unscaledDeltaTime;

            BulletImpactVfx.TickPierceLine(_towerView, deltaTime);
            TickSpawn(deltaTime);
            TickEnemies(deltaTime);
            TickTowerAttack(deltaTime);
            TickBullets(deltaTime);
        }

        private void TickSpawn(float deltaTime)
        {
            if (_enemies.Count >= _settings.MaxAliveEnemies)
                return;

            _spawnRemaining -= deltaTime;
            if (_spawnRemaining > 0f)
                return;

            SpawnEnemy();

            var delayRange = _settings.SpawnDelayRange;
            _spawnRemaining = Random.Range(Mathf.Min(delayRange.x, delayRange.y), Mathf.Max(delayRange.x, delayRange.y));
        }

        private void SpawnEnemy()
        {
            var definition = _settings.EnemyDefinition;
            var angle = Random.Range(0f, Mathf.PI * 2f);
            // От боевого (отъехавшего) кадра, а не от текущего: пока камера доезжает, враг должен
            // уже быть за её краем, иначе он "проявится" посреди кадра.
            var spawnRadius = _stage.CombatFrameRadius * _settings.SpawnRadiusRatio;
            var offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;

            var view = Instantiate(
                definition.ViewPrefab,
                _stage.TowerRoot.position + offset,
                Quaternion.identity,
                _stage.CombatRoot);

            view.speedMoving = definition.Speed * _settings.EnemySpeedMultiplier;
            view.definition = definition;
            view.type = definition.Type;

            if (view.HealthSlider != null)
                view.HealthSlider.value = 1f;

            BulletImpactVfx.SetLayerRecursively(view.gameObject, _stage.PreviewLayer);

            _enemies.Add(new PreviewEnemy(view, definition.Health));
        }

        private void TickEnemies(float deltaTime)
        {
            var towerPos = _stage.TowerRoot.position;

            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];

                if (enemy.View == null)
                {
                    _enemies.RemoveAt(i);
                    continue;
                }

                TickFrost(enemy.View, deltaTime);

                if (enemy.IsDying)
                    continue;

                var enemyPos = enemy.View.transform.position;
                var speed = enemy.View.speedMoving * (enemy.View.frostTimeRemaining > 0f
                    ? enemy.View.frostSpeedMultiplier
                    : 1f);

                enemy.View.transform.position = Vector3.MoveTowards(enemyPos, towerPos, speed * deltaTime);

                // Разворот меша на башню — как в EnemyMovingSystem.
                var angle = Mathf.Atan2(towerPos.y - enemyPos.y, towerPos.x - enemyPos.x) * Mathf.Rad2Deg;
                if (enemy.View.Mesh != null)
                    enemy.View.Mesh.rotation = Quaternion.Euler(0f, 0f, angle);

                // Дошёл до башни — витрина не показывает урон по башне, поэтому просто убираем.
                if (Vector3.Distance(enemy.View.transform.position, towerPos) <= _settings.EnemyDespawnDistance)
                {
                    Destroy(enemy.View.gameObject);
                    _enemies.RemoveAt(i);
                }
            }
        }

        /// <summary>Тот же визуал обледенения, что EnemySpeedModifierSystem крутит в бою.</summary>
        private static void TickFrost(EnemyView view, float deltaTime)
        {
            if (view.frostTimeRemaining <= 0f)
                return;

            view.frostTimeRemaining -= deltaTime;

            if (view.frostTimeRemaining <= 0f)
            {
                view.frostSpeedMultiplier = 1f;
                view.SetFrostVisual(false, 1f);
                return;
            }

            view.SetFrostVisual(true, view.frostSpeedMultiplier);
        }

        private void TickTowerAttack(float deltaTime)
        {
            if (_reloadRemaining > 0f)
            {
                _reloadRemaining -= deltaTime;
                return;
            }

            var target = FindNearestEnemyInRange();
            if (target == null)
                return;

            SpawnBullet(target);
            _reloadRemaining = 1f / Mathf.Max(0.01f, _towerView.attackSpeed);
        }

        private PreviewEnemy FindNearestEnemyInRange()
        {
            PreviewEnemy nearest = null;
            var nearestDistance = float.MaxValue;
            var towerPos = _towerView.transform.position;
            var range = _towerView.attackDistance * _towerView.ratioRange;

            foreach (var enemy in _enemies)
            {
                if (enemy.View == null || enemy.IsDying)
                    continue;

                var distance = Vector3.Distance(towerPos, enemy.View.transform.position);
                if (distance > range || distance >= nearestDistance)
                    continue;

                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void SpawnBullet(PreviewEnemy target)
        {
            var bulletPrefab = _settings.BulletConfigSettings != null
                ? _settings.BulletConfigSettings.PrefabViewBullet
                : null;

            if (bulletPrefab == null)
                return;

            var view = Instantiate(
                bulletPrefab,
                _towerView.transform.position,
                Quaternion.identity,
                _stage.CombatRoot);

            view.attackType = _towerView.attackType;
            view.damage = _towerView.attackDamage;
            view.splashRadius = _towerView.splashRadius;
            view.splashFalloff = _towerView.splashFalloff;
            view.splashImpactEffectPrefab = _towerView.splashImpactEffectPrefab;
            view.splashImpactEffectReferenceRadius = _towerView.splashImpactEffectReferenceRadius;
            view.frostSlowPercent = _towerView.frostSlowPercent;
            view.frostSlowDuration = _towerView.frostSlowDuration;
            view.pierceCount = _towerView.pierceCount;
            view.pierceJumpRadius = _towerView.pierceJumpRadius;
            view.pierceFalloff = _towerView.pierceFalloff;

            BulletImpactVfx.SetLayerRecursively(view.gameObject, _stage.PreviewLayer);

            _bullets.Add(new PreviewBullet(view, target));
        }

        private void TickBullets(float deltaTime)
        {
            var speed = _settings.BulletConfigSettings != null ? _settings.BulletConfigSettings.SpeedMoving : 1.5f;

            for (var i = _bullets.Count - 1; i >= 0; i--)
            {
                var bullet = _bullets[i];

                if (bullet.View == null)
                {
                    _bullets.RemoveAt(i);
                    continue;
                }

                // Цель умерла в полёте — снаряд просто исчезает, как и в бою.
                if (bullet.Target == null || bullet.Target.View == null || bullet.Target.IsDying)
                {
                    Destroy(bullet.View.gameObject);
                    _bullets.RemoveAt(i);
                    continue;
                }

                var targetPos = bullet.Target.View.transform.position;
                bullet.View.transform.position = Vector3.MoveTowards(
                    bullet.View.transform.position,
                    targetPos,
                    speed * deltaTime);

                if (Vector3.Distance(bullet.View.transform.position, targetPos) > _settings.BulletHitDistance)
                    continue;

                ApplyHit(bullet);
                Destroy(bullet.View.gameObject);
                _bullets.RemoveAt(i);
            }
        }

        private void ApplyHit(PreviewBullet bullet)
        {
            var target = bullet.Target;
            var hitPos = target.View.transform.position;

            switch (bullet.View.attackType)
            {
                case EMainTowerAttackType.Splash:
                    BulletImpactVfx.SpawnSplashImpact(
                        bullet.View.splashImpactEffectPrefab,
                        hitPos,
                        bullet.View.splashRadius,
                        bullet.View.splashImpactEffectReferenceRadius,
                        _stage.PreviewLayer);
                    ApplySplashDamage(bullet, hitPos);
                    break;

                case EMainTowerAttackType.Frost:
                    target.View.ApplyFrost(
                        Mathf.Clamp01(1f - bullet.View.frostSlowPercent),
                        bullet.View.frostSlowDuration);
                    break;

                case EMainTowerAttackType.Pierce:
                    ApplyPierce(bullet, target);
                    break;
            }

            DamageEnemy(target, bullet.View.damage);
        }

        private void ApplySplashDamage(PreviewBullet bullet, Vector3 center)
        {
            var splashDamage = Mathf.RoundToInt(bullet.View.damage * bullet.View.splashFalloff);

            for (var i = _enemies.Count - 1; i >= 0; i--)
            {
                var enemy = _enemies[i];

                if (enemy == bullet.Target || enemy.View == null || enemy.IsDying)
                    continue;

                if (Vector3.Distance(center, enemy.View.transform.position) > bullet.View.splashRadius)
                    continue;

                DamageEnemy(enemy, splashDamage);
            }
        }

        private void ApplyPierce(PreviewBullet bullet, PreviewEnemy target)
        {
            _pierceHitPoints.Clear();
            _pierceHitPoints.Add(target.View.transform.position);

            var damage = (float) bullet.View.damage;
            var previous = target;
            var hit = new List<PreviewEnemy> { target };

            for (var i = 0; i < bullet.View.pierceCount - 1; i++)
            {
                damage *= bullet.View.pierceFalloff;

                var next = FindNearestUnhit(previous, hit, bullet.View.pierceJumpRadius);
                if (next == null)
                    break;

                _pierceHitPoints.Add(next.View.transform.position);
                DamageEnemy(next, Mathf.RoundToInt(damage));

                hit.Add(next);
                previous = next;
            }

            BulletImpactVfx.ShowPierceLine(_towerView, _towerView.transform.position, _pierceHitPoints);
        }

        private PreviewEnemy FindNearestUnhit(PreviewEnemy from, List<PreviewEnemy> alreadyHit, float radius)
        {
            PreviewEnemy nearest = null;
            var nearestDistance = float.MaxValue;
            var fromPos = from.View.transform.position;

            foreach (var enemy in _enemies)
            {
                if (enemy.View == null || enemy.IsDying || alreadyHit.Contains(enemy))
                    continue;

                var distance = Vector3.Distance(fromPos, enemy.View.transform.position);
                if (distance > radius || distance >= nearestDistance)
                    continue;

                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        private void DamageEnemy(PreviewEnemy enemy, int damage)
        {
            if (enemy.IsDying || enemy.View == null)
                return;

            enemy.Health -= damage;

            if (enemy.View.HealthSlider != null)
                enemy.View.HealthSlider.value = Mathf.Clamp01((float) enemy.Health / Mathf.Max(1, enemy.MaxHealth));

            if (enemy.Health > 0)
                return;

            KillEnemy(enemy);
        }

        private void KillEnemy(PreviewEnemy enemy)
        {
            enemy.IsDying = true;

            var particlePrefab = _settings.EnemyDefinition.GetRandomParticle();
            if (particlePrefab != null)
            {
                var particles = Instantiate(
                    particlePrefab,
                    enemy.View.transform.position,
                    Quaternion.identity,
                    _stage.CombatRoot);

                BulletImpactVfx.SetLayerRecursively(particles.gameObject, _stage.PreviewLayer);
                Destroy(particles.gameObject, _settings.DeathParticleLifetime);
            }

            enemy.View.PlayDeathAnimation();
            Destroy(enemy.View.gameObject, _settings.EnemyDefinition.DeathDelay);
        }

        private class PreviewEnemy
        {
            public PreviewEnemy(EnemyView view, int health)
            {
                View = view;
                Health = health;
                MaxHealth = Mathf.Max(1, health);
            }

            public EnemyView View { get; }
            public int Health { get; set; }
            public int MaxHealth { get; }
            public bool IsDying { get; set; }
        }

        private class PreviewBullet
        {
            public PreviewBullet(BulletView view, PreviewEnemy target)
            {
                View = view;
                Target = target;
            }

            public BulletView View { get; }
            public PreviewEnemy Target { get; }
        }
    }
}
