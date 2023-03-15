using Systems.Initializable;
using Helpers;
using Signals;
using UnityEngine;
using Zenject;

namespace Installers
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private Camera mainCamera;
        [SerializeField] private SceneHandler sceneHandler;
        
        public override void InstallBindings()
        {
            InitializeSignals();
            
            Container.Bind<Camera>().FromInstance(mainCamera).AsSingle();
            Container.Bind<SceneHandler>().FromInstance(sceneHandler).AsSingle();

            InstallSystems();
        }

        private void InstallSystems()
        {
            Container.BindInterfacesAndSelfTo<TowerInitializeSystem>().AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<GameInitializeSystem>().AsSingle().NonLazy();
        }

        private void InitializeSignals()
        {
            SignalBusInstaller.Install(Container);
            Container.DeclareSignal<InitializeTowerSignal>();
        }
    }
}