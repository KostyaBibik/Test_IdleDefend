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

            // Счётчик уровня — основная защита от превышения maxLevel: сравнение по float-стату
            // (CanUpRange/CanUpAttackSpeed/...) может пропустить лишнюю покупку из-за накопления
            // погрешности после нескольких сложений upgradeValue.
            if (date.currentLevel >= date.upgradeContainer.maxLevel)
                return;

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
                    date.currentLevel++;
                    var rangeView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    rangeView.SetCost(date.currentCostUp);
                    rangeView.SetLevel(date.currentLevel, date.upgradeContainer.maxLevel);

                    break;
                }
                case EUpgradeType.AttackSpeed:
                {
                    if(!_changeAttackSpeedSystem.CanUpAttackSpeed())
                        break;

                    if(!_coinService.TryBought(date.currentCostUp))
                        break;

                    date.currentCostUp += date.upgradeContainer.costUpgrade;
                    date.currentLevel++;
                    _changeAttackSpeedSystem.UpAttackSpeed(date.upgradeContainer.upgradeValue);
                    var speedView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    speedView.SetCost(date.currentCostUp);
                    speedView.SetLevel(date.currentLevel, date.upgradeContainer.maxLevel);
                    break;
                }
                case EUpgradeType.AttackDamage:
                {
                    if(!_changeAttackDamageSystem.CanUpAttackDamage())
                        break;

                    if(!_coinService.TryBought(date.currentCostUp))
                        break;

                    date.currentCostUp += date.upgradeContainer.costUpgrade;
                    date.currentLevel++;
                    _changeAttackDamageSystem.UpAttackDamage((int)date.upgradeContainer.upgradeValue);
                    var damageView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    damageView.SetCost(date.currentCostUp);
                    damageView.SetLevel(date.currentLevel, date.upgradeContainer.maxLevel);
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
                    date.currentLevel++;
                    var healthView = _upgradeViewsHandler.GetViewByType(date.upgradeContainer.upgradeType);
                    healthView.SetCost(date.currentCostUp);
                    healthView.SetLevel(date.currentLevel, date.upgradeContainer.maxLevel);
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
                view.SetLevel(0, container.maxLevel, false);
            }
        }

        private class DateContainer
        {
            public UpgradeContainer upgradeContainer;
            public int currentCostUp;
            public int currentLevel;
        }
    }
}