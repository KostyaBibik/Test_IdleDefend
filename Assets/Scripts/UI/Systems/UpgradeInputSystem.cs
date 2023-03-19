using Enums;
using Services;
using UI.Views.Upgradable;
using UnityEngine.UI;
using Zenject;

namespace UI.Systems
{
    public class UpgradeInputSystem : IInitializable
    {
        private readonly Button _upRadiusBtn;
        private readonly Button _upDamageBtn;
        private readonly Button _upAttackSpeedBtn;
        private readonly UpgradeService _upgradeService;

        public UpgradeInputSystem(
            UpgradeViewsHandler upgradeViewsHandler,
            UpgradeService upgradeService
        )
        {
            _upRadiusBtn = upgradeViewsHandler.GetViewByType(EUpgradeType.Radius).UpgradeBtn;
            _upDamageBtn = upgradeViewsHandler.GetViewByType(EUpgradeType.AttackDamage).UpgradeBtn;
            _upAttackSpeedBtn = upgradeViewsHandler.GetViewByType(EUpgradeType.AttackSpeed).UpgradeBtn;
            
            _upgradeService = upgradeService;
        }

        public void Initialize()
        {
            _upRadiusBtn.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.Radius);
            });
            
            _upDamageBtn.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.AttackDamage);
            });
            
            _upAttackSpeedBtn.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.AttackSpeed);
            });
        }
    }
}