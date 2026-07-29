using Db;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.Actions
{
    public class TowerChangeRadiusSystem : IInitializable
    {
        private readonly TowerView _towerView;
        private readonly int _maxRange;

        private float _currentRange;
        
        public TowerChangeRadiusSystem(
            TowerView towerView,
            TowerConfigSettings towerConfigSettings
        )
        {
            _towerView = towerView;
            _maxRange = towerConfigSettings.MaxRangeAttack;
        }

        public bool CanUpRange()
        {
            return _maxRange > _currentRange;
        }
        
        public void ChangeRadius(float newRadius)
        {
            _currentRange = newRadius;
            _towerView.attackDistance = newRadius;
            _towerView.Sphere.localScale = new Vector3(newRadius, newRadius, newRadius);
        }

        public void UpRadius(float addValue)
        {
            var nextRange = Mathf.Min(_maxRange, _currentRange + addValue);
            var appliedValue = nextRange - _currentRange;

            _currentRange = nextRange;
            _towerView.attackDistance += appliedValue;
            _towerView.Sphere.localScale += new Vector3(appliedValue, appliedValue, appliedValue);
        }

        public void Initialize()
        {
            _currentRange = _towerView.attackDistance;
            ChangeRadius(_towerView.attackDistance);
        }
    }
}
