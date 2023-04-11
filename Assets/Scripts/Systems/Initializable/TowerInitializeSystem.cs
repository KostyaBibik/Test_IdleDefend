using Db;
using Views.Impl;
using Zenject;

namespace Systems.Initializable
{
    public class TowerInitializeSystem : IInitializable
    {
        private readonly TowerConfigSettings _towerConfigSettings;
        private readonly TowerView _towerView;
        
        public TowerInitializeSystem(
            TowerConfigSettings towerConfigSettings,
            TowerView towerView
        )
        {
            _towerConfigSettings = towerConfigSettings;
            _towerView = towerView;
        }
        
        public void Initialize()
        {
            _towerView.attackDistance = _towerConfigSettings.RangeAttack;
            _towerView.attackSpeed = _towerConfigSettings.AttackSpeed;
            _towerView.attackDamage = _towerConfigSettings.AttackDamage;
        }
    }
}