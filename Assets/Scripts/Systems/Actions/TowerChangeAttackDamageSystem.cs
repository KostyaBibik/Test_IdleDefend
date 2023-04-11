using Db;
using Views.Impl;
using Zenject;

namespace Systems.Actions
{
    public class TowerChangeAttackDamageSystem : IInitializable
    {
        private readonly TowerView _towerView;
        private readonly float _maxAttackDamage;

        private int _currentAttackDamage;
        
        public TowerChangeAttackDamageSystem(
            TowerView towerView,
            TowerConfigSettings towerConfigSettings
        )
        {
            _towerView = towerView;
            _maxAttackDamage = towerConfigSettings.MaxAttackDamage;
        }
        
        public bool CanUpAttackDamage()
        {
            return _maxAttackDamage > _currentAttackDamage;
        }
        
        public void ChangeAttackDamage(int newAttackDamage)
        {
            _currentAttackDamage = newAttackDamage;
            _towerView.attackDamage = newAttackDamage;
        }

        public void UpAttackDamage(int addValue)
        {
            _currentAttackDamage += addValue;
            _towerView.attackDamage += addValue;
        }
        
        public void Initialize()
        {
            _currentAttackDamage = _towerView.attackDamage;
            ChangeAttackDamage(_currentAttackDamage);
        }
    }
}