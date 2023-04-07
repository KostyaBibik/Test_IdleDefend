using System;
using Components.Tower;
using Db;
using Signals;
using UI.Views;
using Zenject;

namespace Systems.Actions
{
    public class TowerChangeHealthSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly TowerHealthView _healthView;
        private readonly TowerConfigSettings _towerConfigSettings;
        private readonly TowerHealthComponent _towerHealthComponent;
        
        public TowerChangeHealthSystem(
            SignalBus signalBus,
            TowerHealthView healthView,
            TowerConfigSettings towerConfigSettings,
            TowerHealthComponent towerHealthComponent
            )
        {
            _signalBus = signalBus;
            _healthView = healthView;
            _towerConfigSettings = towerConfigSettings;
            _towerHealthComponent = towerHealthComponent;
        }

        private void TowerLostHealth(TowerLostHealthSignal signal)
        {
            _towerHealthComponent.ReduceHealth(signal.damageCount);
            _healthView.LoseHealth();
        }
        
        private void AddTowerHealth(TowerAddHealthSignal signal)
        {
            _towerHealthComponent.AddHealth(signal.additiveCount);
            _healthView.AddHealth();
        }
        
        public void Initialize()
        {
            var startHealth = _towerConfigSettings.StartHealthCount;
            _healthView.ResetHealth(startHealth);
            _towerHealthComponent.AddHealth(startHealth);
            
            _signalBus.Subscribe<TowerLostHealthSignal>(TowerLostHealth);
            _signalBus.Subscribe<TowerAddHealthSignal>(AddTowerHealth);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<TowerLostHealthSignal>(TowerLostHealth);
            _signalBus.Unsubscribe<TowerAddHealthSignal>(AddTowerHealth);
        }
    }
}