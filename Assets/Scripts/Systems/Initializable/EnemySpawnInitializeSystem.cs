using System;
using System.Collections;
using Db;
using Helpers;
using Infrastructure.Impl;
using Services;
using Systems.RunTime.Camera;
using UniRx;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Systems.Initializable
{
    public class EnemySpawnInitializeSystem : IInitializable, IDisposable
    {
        private readonly EntityFactory _entityFactory;
        private readonly LevelService _levelService;
        private readonly SceneHandler _sceneHandler;
        private readonly CameraZoomSystem _cameraZoomSystem;

        private CompositeDisposable _disposables = new();
        private int _wavesInProgress;

        public EnemySpawnInitializeSystem(
            EntityFactory entityFactory,
            LevelService levelService,
            SceneHandler sceneHandler,
            CameraZoomSystem cameraZoomSystem
        )
        {
            _entityFactory = entityFactory;
            _levelService = levelService;
            _sceneHandler = sceneHandler;
            _cameraZoomSystem = cameraZoomSystem;
        }

        private IEnumerator SpawnWave(WaveDefinition wave)
        {
            try
            {
                var triggerSeconds = _levelService.CurrentLevel.GetTriggerSeconds(wave.startTrigger);
                yield return new WaitUntil(() =>
                    _levelService.ElapsedSeconds >= triggerSeconds || _levelService.IsSpawnCapped);

                for (var i = 0; i < wave.count && !_levelService.IsSpawnCapped; i++)
                {
                    yield return new WaitForSeconds(wave.spawnDelay);

                    if (_levelService.IsSpawnCapped)
                        break;

                    var safeRadius = _cameraZoomSystem.GetSafeSpawnRadius();
                    var distanceFromCenter = Random.Range(safeRadius, safeRadius + 1.5f);
                    var randomPoint = _sceneHandler.TowerPos.position +
                                       new Vector3(Random.value - 0.5f, Random.value - 0.5f, 0f).normalized *
                                       distanceFromCenter;

                    _entityFactory.CreateEnemy(randomPoint, wave.enemyType, wave.extraHealth, wave.extraSpeed);
                }
            }
            finally
            {
                _wavesInProgress--;
                if (_wavesInProgress <= 0)
                    _levelService.NotifyAllWavesSpawned();
            }
        }

        public void Initialize()
        {
            _disposables.Dispose();
            _disposables = new CompositeDisposable();

            var waves = _levelService.CurrentLevel.Waves;
            _wavesInProgress = waves.Count;

            foreach (var wave in waves)
            {
                var capturedWave = wave;
                _disposables.Add(Observable.FromCoroutine(() => SpawnWave(capturedWave)).Subscribe());
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
