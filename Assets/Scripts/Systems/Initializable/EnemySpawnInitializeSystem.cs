using System;
using System.Collections;
using Systems.RunTime.Enemies;
using Db;
using Enums;
using Helpers;
using Infrastructure.Impl;
using UniRx;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Systems.Initializable
{
    public class EnemySpawnInitializeSystem : IInitializable, IDisposable
    {
        private readonly EntityFactory _entityFactory;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private SceneHandler _sceneHandler;
        private readonly IncreasingEnemyParametersSystem _increasingEnemyParameters;

        private IDisposable _observer;
        
        public EnemySpawnInitializeSystem(
            EntityFactory entityFactory,
            EnemyPrefabsConfig prefabsConfig,
            SceneHandler sceneHandler,
            IncreasingEnemyParametersSystem increasingEnemyParametersSystem
        )
        {
            _entityFactory = entityFactory;
            _enemyPrefabsConfig = prefabsConfig;
            _sceneHandler = sceneHandler;
            _increasingEnemyParameters = increasingEnemyParametersSystem;
        }

        private IEnumerator SpawnEnemyWithDelay()
        {
            var delay = new WaitForSeconds(_enemyPrefabsConfig.SpawnDelay);
            
            do
            {
                yield return delay;

                var distanceFromCenter = Random.Range(3f, 5f);
                
                var randomPoint = _sceneHandler.TowerPos.position + new Vector3(Random.value - 0.5f, Random.value - 0.5f, 0f).normalized * distanceFromCenter;

                var randomTypeCounter = Random.Range(0, _enemyPrefabsConfig.CountPrefabs);
                var randomType = (EEnemyType)Enum.GetValues(typeof(EEnemyType)).GetValue(randomTypeCounter);

                var additiveHealth = _increasingEnemyParameters.additiveHealth;
                var additiveSpeed = _increasingEnemyParameters.additiveSpeed;

                _entityFactory.CreateEnemy(randomPoint, randomType, additiveHealth, additiveSpeed);
            } while (true);
        }

        [Inject]
        public void Construct(SceneHandler sceneHandler)
        {
            _sceneHandler = sceneHandler;
        }
        
        public void Initialize()
        {
            _observer?.Dispose();
            
            _observer = Observable.FromCoroutine(SpawnEnemyWithDelay)
                .Subscribe();
        }

        public void Dispose()
        {
            _observer?.Dispose();
        }
    }
}