using Systems.Actions;
using Db;
using Enums;
using Signals;
using UI.Views.Upgradable;
using Views.Impl;
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
        private readonly TowerView _towerView;

        private int costUpgradeRangeAttack;
        private int costUpgradeAttackSpeed;
        private int costUpgradeAttackDamage;
        private int costUpHealth;
        
        public UpgradeService(
            TowerChangeRadiusSystem towerChangeRadiusSystem,
            UpgradeViewsHandler upgradeViewsHandler,
            UpgradeTowerConfigSettings upgradeTowerConfigSettings,
            SignalBus signalBus,
            CoinService coinService,
            TowerView towerView
            )
        {
            _towerChangeRadiusSystem = towerChangeRadiusSystem;
            _upgradeViewsHandler = upgradeViewsHandler;
            _upgradeTowerConfigSettings = upgradeTowerConfigSettings;
            _signalBus = signalBus;
            _coinService = coinService;
            _towerView = towerView;
        }

        public void InvokeUpgrade(EUpgradeType upgradeType)
        {
            var typeContainer = _upgradeTowerConfigSettings.GetContainer(upgradeType);
            switch (upgradeType)
            {
                case EUpgradeType.None:
                    break;
                
                case EUpgradeType.RangeAttack:
                {
                    if(!_coinService.TryBought(costUpgradeRangeAttack))
                        break;
                    
                    _towerChangeRadiusSystem.UpRadius(typeContainer.upgradeValue);
                    costUpgradeRangeAttack += typeContainer.costUpgrade;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.RangeAttack).SetCost(costUpgradeRangeAttack);
                    
                    break;
                }
                case EUpgradeType.AttackSpeed:
                {
                    if(!_coinService.TryBought(costUpgradeAttackSpeed))
                        break;
                    
                    costUpgradeAttackSpeed += typeContainer.costUpgrade;
                    _towerView.attackSpeed += typeContainer.upgradeValue;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackSpeed).SetCost(costUpgradeAttackSpeed);
                    break;
                }
                case EUpgradeType.AttackDamage:
                {
                    if(!_coinService.TryBought(costUpgradeAttackDamage))
                        break;
                    
                    costUpgradeAttackDamage += typeContainer.costUpgrade;
                    _towerView.attackValue += (int)typeContainer.upgradeValue;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackDamage).SetCost(costUpgradeAttackDamage);
                    break;
                }
                case EUpgradeType.UpHealth:
                {
                    if(!_coinService.TryBought(costUpHealth))
                        break;
                    
                    _signalBus.Fire(new TowerAddHealthSignal()
                    {
                        additiveCount = 1
                    });
                    
                    costUpHealth += typeContainer.costUpgrade;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.UpHealth).SetCost(costUpHealth);
                    break;
                }
            }
        }

        public void Initialize()
        {
            var startRangeAttackCost = _upgradeTowerConfigSettings.GetContainer(EUpgradeType.RangeAttack).startCost;
            var startAttackDamageCost = _upgradeTowerConfigSettings.GetContainer(EUpgradeType.AttackDamage).startCost;
            var startAttackSpeedCost = _upgradeTowerConfigSettings.GetContainer(EUpgradeType.AttackSpeed).startCost;
            var startUpHealthCost = _upgradeTowerConfigSettings.GetContainer(EUpgradeType.UpHealth).startCost;
            
            costUpgradeRangeAttack = startRangeAttackCost;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.RangeAttack).SetCost(costUpgradeRangeAttack);

            costUpgradeAttackSpeed = startAttackSpeedCost;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackSpeed).SetCost(costUpgradeAttackSpeed);
            
            costUpgradeAttackDamage = startAttackDamageCost;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackDamage).SetCost(costUpgradeAttackDamage);
            
            costUpHealth = startUpHealthCost;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.UpHealth).SetCost(costUpHealth);
        }
    }
}