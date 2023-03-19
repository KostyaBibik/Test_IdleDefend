using Systems.Actions;
using Systems.RunTime;
using Db;
using Enums;
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
        private readonly CoinService _coinService;
        private readonly TowerView _towerView;

        private int costUpgradeRadius;
        private int costUpgradeAttackSpeed;
        private int costUpgradeAttackDamage;
        
        public UpgradeService(
            TowerChangeRadiusSystem towerChangeRadiusSystem,
            UpgradeViewsHandler upgradeViewsHandler,
            UpgradeTowerConfigSettings upgradeTowerConfigSettings,
            CoinService coinService,
            TowerView towerView
            )
        {
            _towerChangeRadiusSystem = towerChangeRadiusSystem;
            _upgradeViewsHandler = upgradeViewsHandler;
            _upgradeTowerConfigSettings = upgradeTowerConfigSettings;
            _coinService = coinService;
            _towerView = towerView;
        }

        public void InvokeUpgrade(EUpgradeType upgradeType)
        {
            switch (upgradeType)
            {
                case EUpgradeType.None:
                    break;
                
                case EUpgradeType.Radius:
                {
                    if(!_coinService.TryBought(costUpgradeRadius))
                        break;
                    
                    _towerChangeRadiusSystem.UpRadius(_upgradeTowerConfigSettings.UpgradeRangeAttack);
                    costUpgradeRadius += _upgradeTowerConfigSettings.CostRangeUpgrade;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.Radius).SetCost(costUpgradeRadius);
                    
                    break;
                }
                case EUpgradeType.AttackSpeed:
                {
                    if(!_coinService.TryBought(costUpgradeAttackSpeed))
                        break;
                    
                    costUpgradeAttackSpeed += _upgradeTowerConfigSettings.CostAttackSpeedUpgrade;
                    _towerView.attackSpeed += _upgradeTowerConfigSettings.UpgradeAttackSpeed;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackSpeed).SetCost(costUpgradeAttackSpeed);
                    break;
                }
                case EUpgradeType.AttackDamage:
                {
                    if(!_coinService.TryBought(costUpgradeAttackDamage))
                        break;
                    
                    costUpgradeAttackDamage += _upgradeTowerConfigSettings.CostAttackDamageUpgrade;
                    _towerView.attackValue += _upgradeTowerConfigSettings.UpgradeAttackDamage;
                    _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackDamage).SetCost(costUpgradeAttackDamage);
                    break;
                }
            }
        }

        public void Initialize()
        {
            costUpgradeRadius = _upgradeTowerConfigSettings.StartCostRadiusUpgrade;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.Radius).SetCost(costUpgradeRadius);

            costUpgradeAttackSpeed = _upgradeTowerConfigSettings.StartCostAttackSpeedUpgrade;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackSpeed).SetCost(costUpgradeAttackSpeed);
            
            costUpgradeAttackDamage = _upgradeTowerConfigSettings.StartCostAttackDamageUpgrade;
            _upgradeViewsHandler.GetViewByType(EUpgradeType.AttackDamage).SetCost(costUpgradeAttackDamage);
        }
    }
}