using System;
using System.Collections;
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
    public class EnemySpawnInitializeSystem : IInitializable
    {
        private readonly EntityFactory _entityFactory;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly SceneHandler _sceneHandler;
        
        public EnemySpawnInitializeSystem(
            EntityFactory entityFactory,
            EnemyPrefabsConfig prefabsConfig,
            SceneHandler sceneHandler
        )
        {
            _entityFactory = entityFactory;
            _enemyPrefabsConfig = prefabsConfig;
            _sceneHandler = sceneHandler;
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
                
                _entityFactory.CreateEnemy(randomPoint, randomType);
            } while (true);
        }

        public void Initialize()
        {
            Observable.FromCoroutine(SpawnEnemyWithDelay)
                .Subscribe();
        }
    }
}