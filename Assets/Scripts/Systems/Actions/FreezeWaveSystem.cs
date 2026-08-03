using System.Collections.Generic;
using Services;
using Services.Impl;
using Systems.RunTime.Camera;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.Actions
{
    /// <summary>
    /// Визуальное и игровое кольцо ультимейта "Заморозка": расширяется от башни наружу, враг
    /// замерзает в момент, когда фронт волны его касается, а не мгновенно все разом - так игрок
    /// видит связь между визуалом и реальной зоной поражения. Максимальный радиус берём
    /// динамически из CameraZoomSystem.GetSafeSpawnRadius(), а не фиксированным числом - иначе
    /// на сильно отдалённой камере (апгрейды дальности/доп-башни) волна не доходила бы до края
    /// видимой зоны и часть врагов осталась бы незамороженной.
    /// </summary>
    public class FreezeWaveSystem : ITickable
    {
        private const int RingSegments = 48;
        private const float RingWidth = 0.12f;
        private const float MaxRadiusMargin = 2f;
        private const float BurstEffectLifetime = 2f;

        private readonly TowerView _towerView;
        private readonly EnemyService _enemyService;
        private readonly CameraZoomSystem _cameraZoomSystem;
        private readonly IGameTimeProvider _gameTimeProvider;

        // Значение - poolVersion врага на момент заморозки (см. IEntityView.poolVersion): волна
        // растягивается на несколько кадров, а за это время враг может умереть и его EnemyView
        // - переиспользоваться под нового. Без версии новый враг ошибочно считался бы уже
        // замороженным этой же волной и пропускался.
        private readonly Dictionary<EnemyView, int> _hitEnemies = new Dictionary<EnemyView, int>();
        private LineRenderer _ringVisual;

        private bool _active;
        private float _radius;
        private float _maxRadius;

        public FreezeWaveSystem(
            TowerView towerView,
            EnemyService enemyService,
            CameraZoomSystem cameraZoomSystem,
            IGameTimeProvider gameTimeProvider
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _cameraZoomSystem = cameraZoomSystem;
            _gameTimeProvider = gameTimeProvider;
        }

        public void StartWave()
        {
            _active = true;
            _radius = 0f;
            _maxRadius = _cameraZoomSystem.GetSafeSpawnRadius() + MaxRadiusMargin;
            _hitEnemies.Clear();

            EnsureRingVisual();
            _ringVisual.gameObject.SetActive(true);
            DrawRing(0f);

            SpawnBurstEffect();
        }

        public void Tick()
        {
            if (!_active)
                return;

            _radius += _towerView.freezeWaveSpeed * _gameTimeProvider.DeltaTime;
            DrawRing(_radius);

            var origin = _towerView.transform.position;
            foreach (var enemy in _enemyService.Enemies)
            {
                if (_hitEnemies.TryGetValue(enemy, out var hitVersion) && hitVersion == enemy.poolVersion)
                    continue;

                if (Vector3.Distance(origin, enemy.transform.position) > _radius)
                    continue;

                enemy.ApplyFrost(0f, _towerView.freezeDuration);
                _hitEnemies[enemy] = enemy.poolVersion;
            }

            if (_radius < _maxRadius)
                return;

            _active = false;
            _ringVisual.gameObject.SetActive(false);
        }

        private void EnsureRingVisual()
        {
            if (_ringVisual != null)
                return;

            var go = new GameObject("FreezeWaveRing");
            go.transform.SetParent(_towerView.transform, false);

            _ringVisual = go.AddComponent<LineRenderer>();
            _ringVisual.useWorldSpace = false;
            _ringVisual.loop = true;
            _ringVisual.positionCount = RingSegments;
            _ringVisual.widthMultiplier = RingWidth;
            _ringVisual.material = new Material(Shader.Find("Sprites/Default"));
            _ringVisual.startColor = _ringVisual.endColor = new Color(0.6f, 0.9f, 1f, 0.85f);
        }

        private void DrawRing(float radius)
        {
            for (var i = 0; i < RingSegments; i++)
            {
                var angle = i / (float) RingSegments * Mathf.PI * 2f;
                _ringVisual.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }

        private void SpawnBurstEffect()
        {
            if (_towerView.freezeWaveBurstEffectPrefab == null)
                return;

            var instance = Object.Instantiate(_towerView.freezeWaveBurstEffectPrefab, _towerView.transform.position, Quaternion.identity);

            var audioSource = instance.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }

            Object.Destroy(instance, BurstEffectLifetime);
        }
    }
}
