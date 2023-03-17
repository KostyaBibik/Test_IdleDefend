using Systems.Initializable;
using Systems.RunTime;
using Db;
using Helpers;
using Infrastructure.Impl;
using Services;
using Signals;
using UnityEngine;
using Views;
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
            Container.Bind<SceneHandler>().FromInstance(sceneHandler).AsSingle();

            InstallGameSystems();

            BindAndCreateTowerView();

            Container.BindInterfacesAndSelfTo<EntityFactory>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemySpawnSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyService>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemyMovingSystem>().AsSingle().NonLazy();
        }

        private void InstallGameSystems()
        {
            Container.BindInterfacesAndSelfTo<GameInitializeSystem>().AsSingle().NonLazy();
        }

        private void InitializeSignals()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<InitializeTowerSignal>();
        }

        private void BindAndCreateTowerView()
        {
            var towerView = Container.InstantiatePrefabForComponent<TowerView>(
                towerConfigSettings.PrefabViewTower,
                sceneHandler.TowerPos.position,
                Quaternion.identity, 
                null
            );
            
            Container.BindInterfacesAndSelfTo<TowerView>().FromInstance(towerView).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerAttackSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<TowerChangeRadiusSystem>().AsSingle().NonLazy();
        }
    }
}