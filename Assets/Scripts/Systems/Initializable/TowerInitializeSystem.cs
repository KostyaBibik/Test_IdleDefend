using System;
using Db;
using Helpers;
using Signals;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Systems.Initializable
{
    public class TowerInitializeSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly SceneHandler _sceneHandler;
        private readonly TowerConfigSettings _towerConfigSettings;

        public TowerInitializeSystem(
            SignalBus signalBus,
            SceneHandler sceneHandler,
            TowerConfigSettings towerConfigSettings
        )
        {
            _signalBus = signalBus;
            _sceneHandler = sceneHandler;
            _towerConfigSettings = towerConfigSettings;
        }
        
        private void InitializeTower(InitializeTowerSignal initializeTowerSignal)
        {
            Debug.Log("_sceneHandler.pos" + _sceneHandler.TowerPos.position);
            Object.Instantiate(_towerConfigSettings.PrefabViewTower, _sceneHandler.TowerPos.position, Quaternion.identity);
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<InitializeTowerSignal>(InitializeTower);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<InitializeTowerSignal>(InitializeTower);
        }
    }
}