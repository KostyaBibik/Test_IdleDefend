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
        private readonly SideTowerService _sideTowerService;
        [Inject] private SignalBus _signalBus;

        public EntityFactory(
            EnemyPrefabsConfig enemyPrefabsConfig,
            EnemyService enemyService,
            BulletConfigSettings bulletConfigSettings,
            BulletService bulletService,
            SideTowerService sideTowerService
        )
        {
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _enemyService = enemyService;
            _bulletConfigSettings = bulletConfigSettings;
            _bulletService = bulletService;
            _sideTowerService = sideTowerService;
        }
        
        public void CreateEnemy(
            Vector3 posSpawn,
            EEnemyType type,
            int additiveHealth,
            float additiveSpeed
        )
        {
            var enemyDefinition = _enemyPrefabsConfig.GetPrefab(type);
            var enemyView = DiContainerRef.Container.InstantiatePrefabForComponent<EnemyView>(enemyDefinition.ViewPrefab);
            var enemyTransform = enemyView.transform;
            enemyTransform.position = posSpawn;
            enemyTransform.rotation = Quaternion.identity;
            var healthComponent =
                DiContainerRef.Container.InstantiateComponent<EnemyHealthComponent>(enemyView.gameObject);

            var hp = enemyDefinition.Health + additiveHealth;
            var speed = enemyDefinition.Speed + additiveSpeed;

            healthComponent.Initialize(hp, enemyView.HealthSlider, enemyView);
            healthComponent.signalBus = _signalBus;
            healthComponent.definition = enemyDefinition;
            enemyView.healthComponent = healthComponent;
            enemyView.type = type;
            enemyView.speedMoving = speed;
            enemyView.definition = enemyDefinition;

            _enemyService.AddEntityOnService(enemyView);
        }

        public void CreateSideTower(Vector3 posSpawn, SideTowerDefinition definition)
        {
            var sideTowerView =
                DiContainerRef.Container.InstantiatePrefabForComponent<SideTowerView>(definition.ViewPrefab);
            var towerTransform = sideTowerView.transform;
            towerTransform.position = posSpawn;
            towerTransform.rotation = Quaternion.identity;

            sideTowerView.attackDamage = definition.AttackDamage;
            sideTowerView.attackSpeed = definition.AttackSpeed;
            sideTowerView.attackDistance = definition.AttackDistance;

            _sideTowerService.AddEntityOnService(sideTowerView);
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