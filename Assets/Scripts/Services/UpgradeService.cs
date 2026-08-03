using System;
using System.Collections.Generic;
using System.Linq;
using Systems.Actions;
using Components;
using Db;
using Enums;
using Signals;
using UI.Views;
using UI.Views.Upgradable;
using UnityEngine;
using Zenject;

namespace Services
{
    public class UpgradeService : IInitializable, IDisposable, ITickable
    {
        private readonly TowerChangeRadiusSystem _towerChangeRadiusSystem;
        private readonly UpgradeViewsHandler _upgradeViewsHandler;
        private readonly UpgradeTowerConfigSettings _upgradeTowerConfigSettings;
        private readonly SignalBus _signalBus;
        private readonly CoinService _coinService;
        private readonly TowerHealthHandler _towerHealthHandler;
        private readonly TowerChangeAttackSpeedSystem _changeAttackSpeedSystem;
        private readonly TowerChangeAttackDamageSystem _changeAttackDamageSystem;
        
        private List<DateContainer> _dateContainers = new List<DateContainer>();
        private bool _initialStateApplied;
        
        public UpgradeService(
            TowerChangeRadiusSystem towerChangeRadiusSystem,
            UpgradeViewsHandler upgradeViewsHandler,
            UpgradeTowerConfigSettings upgradeTowerConfigSettings,
            TowerChangeAttackSpeedSystem changeAttackSpeedSystem,
            TowerChangeAttackDamageSystem changeAttackDamageSystem,
            SignalBus signalBus,
            CoinService coinService,
            TowerHealthHandler towerHealthHandler
            )
        {
            _towerChangeRadiusSystem = towerChangeRadiusSystem;
            _upgradeViewsHandler = upgradeViewsHandler;
            _upgradeTowerConfigSettings = upgradeTowerConfigSettings;
            _changeAttackSpeedSystem = changeAttackSpeedSystem;
            _changeAttackDamageSystem = changeAttackDamageSystem;
            _signalBus = signalBus;
            _coinService = coinService;
            _towerHealthHandler = towerHealthHandler;
        }

        public void InvokeUpgrade(EUpgradeType upgradeType)
        {
            var typeContainer = _upgradeTowerConfigSettings.GetContainer(upgradeType);

            var date = new DateContainer();
            foreach (var dateContainer in _dateContainers)
            {
                if (dateContainer.upgradeContainer.upgradeType == typeContainer.upgradeType)
                {
                    date = dateContainer;
                    break;
                }
            }

            switch (upgradeType)
            {
                case EUpgradeType.None:
                    break;
                
                case EUpgradeType.RangeAttack:
                {
                    if (!_towerChangeRadiusSystem.CanUpRange())
                        break;

                    var paidCost = date.currentCostUp;
                    if(!_coinService.TryBought(paidCost))
                        break;

                    var upgradeValue = GetFloatUpgradeValue(date);
                    _towerChangeRadiusSystem.UpRadius(upgradeValue);
                    date.currentLevel++;
                    date.currentCostUp = GetUpgradeCost(date);
                    var rangeView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    rangeView.SetCost(date.currentCostUp);
                    rangeView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);
                    RefreshState(date);
                    FirePurchased(upgradeType, date.currentLevel, paidCost);

                    break;
                }
                case EUpgradeType.AttackSpeed:
                {
                    if (!_changeAttackSpeedSystem.CanUpAttackSpeed())
                        break;

                    var paidCost = date.currentCostUp;
                    if(!_coinService.TryBought(paidCost))
                        break;

                    var upgradeValue = GetFloatUpgradeValue(date);
                    date.currentLevel++;
                    _changeAttackSpeedSystem.UpAttackSpeed(upgradeValue);
                    date.currentCostUp = GetUpgradeCost(date);
                    var speedView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    speedView.SetCost(date.currentCostUp);
                    speedView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);
                    RefreshState(date);
                    FirePurchased(upgradeType, date.currentLevel, paidCost);
                    break;
                }
                case EUpgradeType.AttackDamage:
                {
                    if (!_changeAttackDamageSystem.CanUpAttackDamage())
                        break;

                    var paidCost = date.currentCostUp;
                    if(!_coinService.TryBought(paidCost))
                        break;

                    var upgradeValue = GetIntUpgradeValue(date);
                    date.currentLevel++;
                    _changeAttackDamageSystem.UpAttackDamage(upgradeValue);
                    date.currentCostUp = GetUpgradeCost(date);
                    var damageView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    damageView.SetCost(date.currentCostUp);
                    damageView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);
                    RefreshState(date);
                    FirePurchased(upgradeType, date.currentLevel, paidCost);
                    break;
                }
                case EUpgradeType.UpHealth:
                {
                    // Потолок жизней (TowerConfigSettings.MaxHealthCounts) раньше не проверялся
                    // нигде, и здоровье можно было докупать бесконечно.
                    if (!_towerHealthHandler.CanUpHealth())
                        break;

                    var paidCost = date.currentCostUp;
                    if(!_coinService.TryBought(paidCost))
                        break;

                    var upgradeValue = GetIntUpgradeValue(date);
                    _signalBus.Fire(new TowerAddHealthSignal
                    {
                        additiveCount = upgradeValue
                    });

                    date.currentLevel++;
                    date.currentCostUp = GetUpgradeCost(date);
                    var healthView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    healthView.SetCost(date.currentCostUp);
                    healthView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);
                    RefreshState(date);
                    FirePurchased(upgradeType, date.currentLevel, paidCost);
                    break;
                }
            }
        }

        private void FirePurchased(EUpgradeType upgradeType, int level, int paidCost)
        {
            _signalBus.Fire(new TowerUpgradePurchasedSignal
            {
                upgradeType = upgradeType,
                level = level,
                paidCost = paidCost
            });
        }

        public void Initialize()
        {
            foreach (var enumType in Enum.GetValues(typeof(EUpgradeType)).Cast<EUpgradeType>().ToList())
            {
                if(enumType == EUpgradeType.None)
                    continue;

                var container = _upgradeTowerConfigSettings.GetContainer(enumType);
                var dateContainer = new DateContainer
                {
                    upgradeContainer = container,
                    currentCostUp = container.startCost,
                    currentLevel = 0
                };

                _dateContainers.Add(dateContainer);
                var view = _upgradeViewsHandler.GetViewByType(enumType);
                view.SetCost(container.startCost);
                view.SetLevel(0, container.CycleLength, false);
            }

            _coinService.onUpdateCountCoins += OnCoinsChanged;
            _signalBus.Subscribe<TowerAddHealthSignal>(OnTowerHealthChanged);
            _signalBus.Subscribe<TowerLostHealthSignal>(OnTowerHealthChanged);
        }

        public void Dispose()
        {
            _coinService.onUpdateCountCoins -= OnCoinsChanged;
            _signalBus.Unsubscribe<TowerAddHealthSignal>(OnTowerHealthChanged);
            _signalBus.Unsubscribe<TowerLostHealthSignal>(OnTowerHealthChanged);
        }

        /// <summary>
        /// Initialize() у разных сервисов (монеты, здоровье, статы) выполняется в порядке
        /// биндингов в GameInstaller — полагаться на этот порядок хрупко. К первому Tick()
        /// все Initialize() уже гарантированно отработали, поэтому финальный пересчёт статуса
        /// кнопок делаем именно здесь, один раз.
        /// </summary>
        public void Tick()
        {
            if (_initialStateApplied)
                return;

            _initialStateApplied = true;

            foreach (var date in _dateContainers)
                RefreshState(date);
        }

        private void OnCoinsChanged(int _)
        {
            foreach (var date in _dateContainers)
                RefreshState(date);
        }

        private void OnTowerHealthChanged(TowerAddHealthSignal _) => RefreshHealthState();
        private void OnTowerHealthChanged(TowerLostHealthSignal _) => RefreshHealthState();

        private void RefreshHealthState()
        {
            foreach (var date in _dateContainers)
            {
                if (date.upgradeContainer.upgradeType == EUpgradeType.UpHealth)
                {
                    RefreshState(date);
                    break;
                }
            }
        }

        /// <summary>
        /// Приводит кнопку апгрейда в актуальное состояние: "MAX" + disabled, если стат уже
        /// на потолке, иначе просто disabled, если не хватает монет.
        /// </summary>
        private void RefreshState(DateContainer date)
        {
            var view = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
            if (view == null)
                return;

            var isMaxed = !CanUpgrade(date.upgradeContainer.upgradeType);
            var canAfford = _coinService.CurrentCoins >= date.currentCostUp;
            view.RefreshState(isMaxed, canAfford);
        }

        private bool CanUpgrade(EUpgradeType upgradeType)
        {
            return upgradeType switch
            {
                EUpgradeType.RangeAttack => _towerChangeRadiusSystem.CanUpRange(),
                EUpgradeType.AttackSpeed => _changeAttackSpeedSystem.CanUpAttackSpeed(),
                EUpgradeType.AttackDamage => _changeAttackDamageSystem.CanUpAttackDamage(),
                EUpgradeType.UpHealth => _towerHealthHandler.CanUpHealth(),
                _ => false
            };
        }

        private int GetUpgradeCost(DateContainer date)
        {
            var container = date.upgradeContainer;
            var rank = GetRank(date);
            var baseCost = container.startCost + date.currentLevel * container.costUpgrade;

            return Mathf.Max(1, Mathf.RoundToInt(baseCost * Mathf.Pow(container.CostMultiplier, rank)));
        }

        private float GetFloatUpgradeValue(DateContainer date)
        {
            var container = date.upgradeContainer;
            return container.upgradeValue * Mathf.Pow(container.ValueMultiplier, GetRank(date));
        }

        private int GetIntUpgradeValue(DateContainer date)
        {
            return Mathf.Max(1, Mathf.RoundToInt(GetFloatUpgradeValue(date)));
        }

        private static int GetRank(DateContainer date)
        {
            return date.currentLevel / date.upgradeContainer.CycleLength;
        }

        private class DateContainer
        {
            public UpgradeContainer upgradeContainer;
            public int currentCostUp;
            public int currentLevel;
        }
    }
}
