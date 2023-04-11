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
        
        private int costUpgradeRangeAttack;
        private int costUpgradeAttackSpeed;
        private int costUpgradeAttackDamage;
        private int costUpHealth;

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
                    if(!_towerChangeRadiusSystem.CanUpRange())
                        break;
                    
                    if(!_coinService.TryBought(date.currentCostUp))
                        break;

                    _towerChangeRadiusSystem.UpRadius(date.upgradeContainer.upgradeValue);
                    date.currentCostUp += date.upgradeContainer.costUpgrade;
                    _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType).SetCost(date.currentCostUp);
                    
                    break;
                }
                case EUpgradeType.AttackSpeed:
                {
                    if(!_changeAttackSpeedSystem.CanUpAttackSpeed())
                        break;
                    
                    if(!_coinService.TryBought(date.currentCostUp))
                        break;
                    
                    date.currentCostUp += date.upgradeContainer.costUpgrade;
                    _changeAttackSpeedSystem.UpAttackSpeed(date.upgradeContainer.upgradeValue);
                    _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType).SetCost(date.currentCostUp);
                    break;
                }
                case EUpgradeType.AttackDamage:
                {
                    if(!_changeAttackDamageSystem.CanUpAttackDamage())
                        break;
                    
                    if(!_coinService.TryBought(date.currentCostUp))
                        break;
                    
                    date.currentCostUp += date.upgradeContainer.costUpgrade;
                    _changeAttackDamageSystem.UpAttackDamage((int)date.upgradeContainer.upgradeValue);
                    _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType).SetCost(date.currentCostUp);
                    break;
                }
                case EUpgradeType.UpHealth:
                {
                    if(!_towerHealthHandler.CanUpHealth())
                        break;
                    
                    if(!_coinService.TryBought(date.currentCostUp))
                        break;
                    
                    _signalBus.Fire(new TowerAddHealthSignal
                    {
                        additiveCount = 1
                    });
                    
                    date.currentCostUp += date.upgradeContainer.costUpgrade;
                    _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType).SetCost(date.currentCostUp);
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
                    currentCostUp = container.startCost
                };
                
                _dateContainers.Add(dateContainer);
                _upgradeViewsHandler.GetViewByType(enumType).SetCost(container.startCost);
            }
        }

        private class DateContainer
        {
            public UpgradeContainer upgradeContainer;
            public int currentCostUp;
        }
    }
}