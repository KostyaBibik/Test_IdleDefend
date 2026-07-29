using System;
using Components.Tower;
using Db;
using Services;
using Signals;
using UI.Views;
using UnityEngine;
using Zenject;

namespace Systems.Actions
{
    public class TowerChangeHealthSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly TowerHealthHandler _healthHandler;
        private readonly TowerConfigSettings _towerConfigSettings;
        private readonly TowerHealthComponent _towerHealthComponent;
        private readonly ActiveBoostService _activeBoostService;
        
        public TowerChangeHealthSystem(
            SignalBus signalBus,
            TowerHealthHandler healthHandler,
            TowerConfigSettings towerConfigSettings,
            TowerHealthComponent towerHealthComponent,
            ActiveBoostService activeBoostService
            )
        {
            _signalBus = signalBus;
            _healthHandler = healthHandler;
            _towerConfigSettings = towerConfigSettings;
            _towerHealthComponent = towerHealthComponent;
            _activeBoostService = activeBoostService;
        }

        private void TowerLostHealth(TowerLostHealthSignal signal)
        {
            if (_activeBoostService.TryBlockTowerDamage())
                return;

            var damage = Mathf.Max(1, signal.damageCount);

            _towerHealthComponent.ReduceHealth(damage);

            // Хендлер гасит ровно одно сердце за вызов, поэтому при damageToTower > 1 (танк)
            // его надо дёрнуть столько же раз, иначе UI разойдётся с реальным счётчиком жизней
            // и CanUpHealth начнёт врать.
            for (var i = 0; i < damage; i++)
                _healthHandler.LoseHealth();
        }

        private void AddTowerHealth(TowerAddHealthSignal signal)
        {
            var amount = Mathf.Max(1, signal.additiveCount);

            _towerHealthComponent.AddHealth(amount);

            for (var i = 0; i < amount; i++)
                _healthHandler.AddHealth();
        }
        
        public void Initialize()
        {
            // Раньше бой всегда стартовал с MaxHealthCounts жизней, а StartHealthCount не использовался
            // вообще. Теперь стартовое здоровье и потолок прокачки — два разных параметра конфига.
            var startHealth = Mathf.Clamp(
                _towerConfigSettings.StartHealthCount, 1, _towerConfigSettings.MaxHealthCounts);
            var maxHealth = _towerConfigSettings.MaxHealthCounts;
            var healthPrefab = _towerConfigSettings.TowerHealthView;

            _healthHandler.InitializeHealths(startHealth, maxHealth, healthPrefab);
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
