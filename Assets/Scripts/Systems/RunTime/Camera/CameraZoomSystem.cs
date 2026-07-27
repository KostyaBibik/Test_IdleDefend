using Db;
using Helpers;
using Services;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Camera
{
    /// <summary>
    /// Авто-отдаление камеры под текущий радиус атаки башни, разблокированные слоты доп-башен
    /// и уже купленные доп-башни. Всегда плавно (лерп). Кадр сдвигается вверх, чтобы контент
    /// не заезжал под нижнюю панель апгрейдов (которая реально блокирует клики).
    /// </summary>
    public class CameraZoomSystem : IInitializable, ITickable
    {
        private readonly UnityEngine.Camera _camera;
        private readonly TowerView _towerView;
        private readonly SceneHandler _sceneHandler;
        private readonly SideTowerService _sideTowerService;
        private readonly LevelService _levelService;
        private readonly CameraZoomSettings _settings;
        private readonly TowerConfigSettings _towerConfigSettings;
        private readonly IGameTimeProvider _gameTimeProvider;

        private float _minOrthographicSize;
        private float _currentVerticalOffset;

        /// <summary>Задел под будущий ручной pinch-zoom поверх авто-минимума. Пока не используется.</summary>
        public float ManualZoomMultiplier { get; set; } = 1f;

        public CameraZoomSystem(
            UnityEngine.Camera camera,
            TowerView towerView,
            SceneHandler sceneHandler,
            SideTowerService sideTowerService,
            LevelService levelService,
            CameraZoomSettings settings,
            TowerConfigSettings towerConfigSettings,
            IGameTimeProvider gameTimeProvider
        )
        {
            _camera = camera;
            _towerView = towerView;
            _sceneHandler = sceneHandler;
            _sideTowerService = sideTowerService;
            _levelService = levelService;
            _settings = settings;
            _towerConfigSettings = towerConfigSettings;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Initialize()
        {
            _minOrthographicSize = _camera.orthographicSize;
        }

        public void Tick()
        {
            var requiredRadius = ComputeRequiredRadius() * _settings.RadiusMargin;

            var sizeFromWidth = requiredRadius / _camera.aspect;
            var sizeFromHeight = requiredRadius / (1f - _settings.BottomUiHeightRatio);
            var sizeFromMainTowerRange = ComputeMainTowerRangeZoomSize();
            var targetSize = Mathf.Clamp(
                Mathf.Max(sizeFromWidth, sizeFromHeight, sizeFromMainTowerRange, _minOrthographicSize) * Mathf.Max(1f, ManualZoomMultiplier),
                _minOrthographicSize, _settings.MaxOrthographicSize);

            var deltaTime = _gameTimeProvider.DeltaTime;
            _camera.orthographicSize = Mathf.Lerp(_camera.orthographicSize, targetSize, deltaTime * _settings.ZoomLerpSpeed);
            _currentVerticalOffset = _camera.orthographicSize * _settings.BottomUiHeightRatio;

            var pos = _camera.transform.position;
            var targetY = _towerView.transform.position.y - _currentVerticalOffset;
            pos.y = Mathf.Lerp(pos.y, targetY, deltaTime * _settings.ZoomLerpSpeed);
            _camera.transform.position = pos;
        }

        private float ComputeRequiredRadius()
        {
            var towerPos = _towerView.transform.position;
            var required = _towerView.attackDistance;

            var unlockedIndices = _levelService.CurrentLevel.UnlockedSideTowerSlotIndices;
            var markers = _sceneHandler.SideTowerSlotMarkers;
            if (unlockedIndices != null && markers != null)
            {
                foreach (var index in unlockedIndices)
                {
                    if (index < 0 || index >= markers.Length)
                        continue;

                    var distance = Vector3.Distance(towerPos, markers[index].position);
                    required = Mathf.Max(required, distance);
                }
            }

            foreach (var tower in _sideTowerService.Towers)
            {
                var distance = Vector3.Distance(towerPos, tower.transform.position) + tower.attackDistance;
                required = Mathf.Max(required, distance);
            }

            return required;
        }

        private float ComputeMainTowerRangeZoomSize()
        {
            var rangeDelta = Mathf.Max(0f, _towerView.attackDistance - _towerConfigSettings.RangeAttack);
            return _minOrthographicSize + rangeDelta * _settings.MainTowerRangeZoomSizePerUnit;
        }

        /// <summary>
        /// Радиус (от башни), за пределами которого гарантированно ничего не видно при текущем
        /// зуме камеры. Используется спавном врагов, чтобы они не появлялись на глазах у игрока.
        /// </summary>
        public float GetSafeSpawnRadius()
        {
            var halfWidth = _camera.orthographicSize * _camera.aspect;
            var maxVerticalExtent = _camera.orthographicSize + _currentVerticalOffset;
            var enclosingRadius = Mathf.Sqrt(halfWidth * halfWidth + maxVerticalExtent * maxVerticalExtent);
            return enclosingRadius * _settings.RadiusMargin;
        }
    }
}
