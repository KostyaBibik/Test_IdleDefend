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
    public class UpgradeService : IInitializable
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

                    if(!_coinService.TryBought(date.currentCostUp))
                        break;

                    var upgradeValue = GetFloatUpgradeValue(date);
                    _towerChangeRadiusSystem.UpRadius(upgradeValue);
                    date.currentLevel++;
                    date.currentCostUp = GetUpgradeCost(date);
                    var rangeView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    rangeView.SetCost(date.currentCostUp);
                    rangeView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);

                    break;
                }
                case EUpgradeType.AttackSpeed:
                {
                    if (!_changeAttackSpeedSystem.CanUpAttackSpeed())
                        break;

                    if(!_coinService.TryBought(date.currentCostUp))
                        break;

                    var upgradeValue = GetFloatUpgradeValue(date);
                    date.currentLevel++;
                    _changeAttackSpeedSystem.UpAttackSpeed(upgradeValue);
                    date.currentCostUp = GetUpgradeCost(date);
                    var speedView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    speedView.SetCost(date.currentCostUp);
                    speedView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);
                    break;
                }
                case EUpgradeType.AttackDamage:
                {
                    if (!_changeAttackDamageSystem.CanUpAttackDamage())
                        break;

                    if(!_coinService.TryBought(date.currentCostUp))
                        break;

                    var upgradeValue = GetIntUpgradeValue(date);
                    date.currentLevel++;
                    _changeAttackDamageSystem.UpAttackDamage(upgradeValue);
                    date.currentCostUp = GetUpgradeCost(date);
                    var damageView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    damageView.SetCost(date.currentCostUp);
                    damageView.SetLevel(date.currentLevel, date.upgradeContainer.CycleLength);
                    break;
                }
                case EUpgradeType.UpHealth:
                {
                    // Потолок жизней (TowerConfigSettings.MaxHealthCounts) раньше не проверялся
                    // нигде, и здоровье можно было докупать бесконечно.
                    if (!_towerHealthHandler.CanUpHealth())
                        break;

                    if(!_coinService.TryBought(date.currentCostUp))
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
                    break;
                }
            }
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
