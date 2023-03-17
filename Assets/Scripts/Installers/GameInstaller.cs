using Systems.Initializable;
using Systems.RunTime;
using Db;
using Helpers;
using Infrastructure.Impl;
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

            InstallSystems();

            BindAndCreateTowerView();

            Container.BindInterfacesAndSelfTo<EntityFactory>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<EnemySpawnSystem>().AsSingle().NonLazy();
        }

        private void InstallSystems()
        {
            Container.BindInterfacesAndSelfTo<GameInitializeSystem>().AsSingle().NonLazy();

            Container.BindInterfacesAndSelfTo<TowerChangeRadiusSystem>().AsSingle().NonLazy();
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
        }
    }
}