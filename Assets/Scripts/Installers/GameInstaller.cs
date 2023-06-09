﻿using Systems.Actions;
using Systems.Initializable;
using Systems.RunTime;
using Systems.RunTime.Bullets;
using Systems.RunTime.Enemies;
using Systems.RunTime.Tower;
using Components.Tower;
using Db;
using Helpers;
using Infrastructure.Impl;
using Services;
using Services.Impl;
using Signals;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private TowerConfigSettings towerConfigSettings;
        [SerializeField] private Camera mainCamera;
        [SerializeField] private SceneHandler sceneHandler;
        
        public override void InstallBindings()
        {
            InitializeSignals();
            
            Container.Bind<Camera>().FromInstance(mainCamera).AsSingle();
            
            Container.Rebind<SceneHandler>().FromInstance(sceneHandler).AsTransient();

            InstallGameSystems();

            BindAndCreateTowerView();

            Container.BindInterfacesAndSelfTo<EntityFactory>().AsSingle().NonLazy();

            BindEnemyComponents();

            BindBulletComponents();

            BindServices();

            Container.Bind<GameInstaller>().FromInstance(this).AsSingle();
        }

        private void InitializeSignals()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<InitializeTowerSignal>();
            Container.DeclareSignal<GameLoseSignal>();
            Container.DeclareSignal<TowerLostHealthSignal>();
            Container.DeclareSignal<TowerAddHealthSignal>();
            Container.DeclareSignal<DestroyEntitySignal>();
            Container.DeclareSignal<ShowRewardSignal>();
        }
        
        private void InstallGameSystems()
        {
            Container.BindInterfacesAndSelfTo<GameInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<LoseActionSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ShowRewardSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<ShakeCamOnDamageSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<IncreasingEnemyParametersSystem>().AsSingle().NonLazy();
        }

        private void BindAndCreateTowerView()
        {
            var towerView = Container.InstantiatePrefabForComponent<TowerView>(
                towerConfigSettings.PrefabViewTower,
                sceneHandler.TowerPos.position,
                Quaternion.identity, 
                null
            );
            
            Container.BindInterfacesAndSelfTo<TowerView>().FromInstance(towerView).AsCached().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerAttackSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeRadiusSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeHealthSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeAttackSpeedSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeAttackDamageSystem>().AsSingle().NonLazy();
            var healthComponent = Container.InstantiateComponent<TowerHealthComponent>(towerView.gameObject);
            Container.BindInterfacesAndSelfTo<TowerHealthComponent>().FromInstance(healthComponent).AsSingle().NonLazy();
        }
        
        private void BindEnemyComponents()
        {
            Container.BindInterfacesAndSelfTo<EnemySpawnInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyMovingSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyCheckToHitSystem>().AsSingle().NonLazy();
        }
        
        private void BindBulletComponents()
        {
            Container.BindInterfacesAndSelfTo<BulletService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<BulletMovingSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<BulletHitSystem>().AsSingle().NonLazy();
        }
        
        private void BindServices()
        {
            Container.BindInterfacesAndSelfTo<UpgradeService>().AsSingle();
            Container.BindInterfacesAndSelfTo<CoinService>().AsSingle();
        }
    }
}