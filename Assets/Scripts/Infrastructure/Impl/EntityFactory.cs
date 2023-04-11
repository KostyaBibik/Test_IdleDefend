using Components.Enemy;
using Db;
using Enums;
using Installers;
using Services.Impl;
using UnityEngine;
using Views;
using Views.Impl;
using Zenject;

namespace Infrastructure.Impl
{
    public class EntityFactory : IFactory
    {
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly EnemyService _enemyService;
        private readonly BulletConfigSettings _bulletConfigSettings;
        private readonly BulletService _bulletService;
        [Inject] private SignalBus _signalBus;
        
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
        
        public void CreateEnemy(
            Vector3 posSpawn,
            EEnemyType type,
            int additiveHealth,
            float additiveSpeed
        )
        {
            var enemyPrefab = _enemyPrefabsConfig.GetPrefab(type);
            var enemyView = DiContainerRef.Container.InstantiatePrefabForComponent<EnemyView>(enemyPrefab.view);
            var enemyTransform = enemyView.transform;
            enemyTransform.position = posSpawn;
            enemyTransform.rotation = Quaternion.identity;
            var healthComponent =
                DiContainerRef.Container.InstantiateComponent<EnemyHealthComponent>(enemyView.gameObject);

            var hp = enemyPrefab.startHealth + additiveHealth;
            var speed = enemyPrefab.speedMoving + additiveSpeed;

            healthComponent.Initialize(hp, enemyView.HealthSlider, enemyView);
            healthComponent.signalBus = _signalBus;
            enemyView.healthComponent = healthComponent;
            enemyView.type = type;
            enemyView.speedMoving = speed;
            
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