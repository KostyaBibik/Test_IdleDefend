using Components;
using Db;
using Installers;
using Services;
using Unity.VisualScripting;
using UnityEngine;
using Views;

namespace Infrastructure.Impl
{
    public class EntityFactory : IFactory
    {
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly EnemyService _enemyService;

        public EntityFactory(
            EnemyPrefabsConfig enemyPrefabsConfig,
            EnemyService enemyService
        )
        {
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _enemyService = enemyService;
        }
        
        public void CreateEnemy(Vector3 posSpawn)
        {
            var enemyView = DiContainerRef.Container.InstantiatePrefabForComponent<EnemyView>(_enemyPrefabsConfig.EnemyView);
            var enemyTransform = enemyView.transform;
            enemyTransform.position = posSpawn;
            enemyTransform.rotation = Quaternion.identity;
            var healthComponent = enemyView.AddComponent<HealthComponent>();
            healthComponent.Initialize(100, 100, enemyView.HealthSlider);
            enemyView.healthComponent = healthComponent;
            _enemyService.AddEnemyOnService(enemyView);
        }
    }
}