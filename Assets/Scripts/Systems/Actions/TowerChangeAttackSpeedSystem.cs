using Db;
using Views.Impl;
using Zenject;

namespace Systems.Actions
{
    public class TowerChangeAttackSpeedSystem : IInitializable
    {
        private readonly TowerView _towerView;
        private readonly float _maxAttackSpeed;

        private float _currentAttackSpeed;
        
        public TowerChangeAttackSpeedSystem(
            TowerView towerView,
            TowerConfigSettings towerConfigSettings
        )
        {
            _towerView = towerView;
            _maxAttackSpeed = towerConfigSettings.MaxAttackSpeed;
        }
        
        public bool CanUpAttackSpeed()
        {
            return _maxAttackSpeed > _currentAttackSpeed;
        }
        
        public void ChangeAttackSpeed(float newAttackSpeed)
        {
            _currentAttackSpeed = newAttackSpeed;
            _towerView.attackSpeed = newAttackSpeed;
        }

        public void UpAttackSpeed(float addValue)
        {
            _currentAttackSpeed += addValue;
            _towerView.attackSpeed += addValue;
        }
        
        public void Initialize()
        {
            _currentAttackSpeed = _towerView.attackSpeed;
            ChangeAttackSpeed(_currentAttackSpeed);
        }
    }
}