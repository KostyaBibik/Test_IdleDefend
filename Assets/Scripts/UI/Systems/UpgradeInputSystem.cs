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
        private readonly Button _upHealthButton;
        private readonly UpgradeService _upgradeService;

        public UpgradeInputSystem(
            UpgradeViewsHandler upgradeViewsHandler,
            UpgradeService upgradeService
        )
        {
            _upRadiusBtn = upgradeViewsHandler.GetViewByType(EUpgradeType.RangeAttack).UpgradeBtn;
            _upDamageBtn = upgradeViewsHandler.GetViewByType(EUpgradeType.AttackDamage).UpgradeBtn;
            _upAttackSpeedBtn = upgradeViewsHandler.GetViewByType(EUpgradeType.AttackSpeed).UpgradeBtn;
            _upHealthButton = upgradeViewsHandler.GetViewByType(EUpgradeType.UpHealth).UpgradeBtn;
            
            _upgradeService = upgradeService;
        }

        public void Initialize()
        {
            _upRadiusBtn.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.RangeAttack);
            });
            
            _upDamageBtn.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.AttackDamage);
            });
            
            _upAttackSpeedBtn.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.AttackSpeed);
            });
            
            _upHealthButton.onClick.AddListener(delegate
            {
                _upgradeService.InvokeUpgrade(EUpgradeType.UpHealth);
            });
        }
    }
}