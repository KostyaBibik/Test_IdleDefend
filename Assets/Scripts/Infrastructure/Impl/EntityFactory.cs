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
        private readonly IEntityPoolService _entityPoolService;
        [Inject] private SignalBus _signalBus;

        public EntityFactory(
            EnemyPrefabsConfig enemyPrefabsConfig,
            EnemyService enemyService,
            BulletConfigSettings bulletConfigSettings,
            BulletService bulletService,
            SideTowerService sideTowerService,
            IEntityPoolService entityPoolService
        )
        {
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _enemyService = enemyService;
            _bulletConfigSettings = bulletConfigSettings;
            _bulletService = bulletService;
            _sideTowerService = sideTowerService;
            _entityPoolService = entityPoolService;
        }

        public EnemyView CreateEnemy(
            Vector3 posSpawn,
            EEnemyType type,
            int additiveHealth,
            float additiveSpeed,
            int experienceRewardOverride = -1,
            bool grantCoinReward = true,
            bool grantExperienceReward = true,
            float healthMultiplier = 1f
        )
        {
            var enemyDefinition = _enemyPrefabsConfig.GetPrefab(type);
            var enemyView = _entityPoolService.Rent(enemyDefinition.ViewPrefab, posSpawn, Quaternion.identity);

            // Компонент здоровья переиспользуется вместе с GameObject-ом (не AddComponent на
            // каждый спавн) - иначе на переиспользованном враге копился бы новый HealthComponent
            // поверх старого при каждом Rent.
            var healthComponent = enemyView.GetComponent<EnemyHealthComponent>();
            if (healthComponent == null)
                healthComponent = DiContainerRef.Container.InstantiateComponent<EnemyHealthComponent>(enemyView.gameObject);

            var hp = Mathf.RoundToInt((enemyDefinition.Health + additiveHealth) * healthMultiplier);
            var speed = enemyDefinition.Speed + additiveSpeed;

            healthComponent.Initialize(hp, enemyView.HealthSlider, enemyView);
            healthComponent.signalBus = _signalBus;
            healthComponent.definition = enemyDefinition;
            enemyView.healthComponent = healthComponent;
            enemyView.type = type;
            enemyView.speedMoving = speed;
            enemyView.definition = enemyDefinition;
            enemyView.experienceRewardOverride = experienceRewardOverride;
            enemyView.grantCoinReward = grantCoinReward;
            enemyView.grantExperienceReward = grantExperienceReward;

            _enemyService.AddEntityOnService(enemyView);
            return enemyView;
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

            sideTowerView.attackType = definition.AttackType;
            sideTowerView.beamPercentMaxHealthPerSecond = definition.BeamPercentMaxHealthPerSecond;
            sideTowerView.chainJumpCount = definition.ChainJumpCount;
            sideTowerView.chainJumpRadius = definition.ChainJumpRadius;
            sideTowerView.chainFalloffFactor = definition.ChainFalloffFactor;
            sideTowerView.chainVisualDuration = definition.ChainVisualDuration;
            sideTowerView.slowPercent = definition.SlowPercent;

            _sideTowerService.AddEntityOnService(sideTowerView);
        }

        public IEntityView CreateBullet(Vector3 posSpawn, BulletView prefabOverride = null)
        {
            var prefab = prefabOverride != null ? prefabOverride : _bulletConfigSettings.PrefabViewBullet;
            var bulletView = _entityPoolService.Rent(prefab, posSpawn, Quaternion.identity);

            _bulletService.AddEntityOnService(bulletView);

            return bulletView;
        }
    }
}
