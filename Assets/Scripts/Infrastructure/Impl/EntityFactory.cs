using Components.Impl;
using Db;
using Installers;
using Services;
using Services.Impl;
using UnityEngine;
using Views;
using Views.Impl;

namespace Infrastructure.Impl
{
    public class EntityFactory : IFactory
    {
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly EnemyService _enemyService;
        private readonly BulletConfigSettings _bulletConfigSettings;
        private readonly BulletService _bulletService;
        
        public EntityFactory(
            EnemyPrefabsConfig enemyPrefabsConfig,
            EnemyService enemyService,
            BulletConfigSettings bulletConfigSettings,
            BulletService bulletService
        )
        {
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _enemyService = enemyService;
            _bulletConfigSettings = bulletConfigSettings;
            _bulletService = bulletService;
        }
        
        public void CreateEnemy(Vector3 posSpawn)
        {
            var enemyView = DiContainerRef.Container.InstantiatePrefabForComponent<EnemyView>(_enemyPrefabsConfig.EnemyView);
            var enemyTransform = enemyView.transform;
            enemyTransform.position = posSpawn;
            enemyTransform.rotation = Quaternion.identity;
            var healthComponent =
                DiContainerRef.Container.InstantiateComponent<EnemyHealthComponent>(enemyView.gameObject);
            healthComponent.Initialize(100, 100, enemyView.HealthSlider, enemyView);
            enemyView.healthComponent = healthComponent;
            _enemyService.AddEntityOnService(enemyView);
        }

        public IEntityView CreateBullet(Vector3 posSpawn)
        {
            var bulletView = DiContainerRef.Container.InstantiatePrefabForComponent<BulletView>(_bulletConfigSettings.PrefabViewBullet);
            var bulletTransform = bulletView.transform;
            bulletTransform.position = posSpawn;
            bulletTransform.rotation = Quaternion.identity;

            _bulletService.AddEntityOnService(bulletView);
            return bulletView;
        }
    }
}