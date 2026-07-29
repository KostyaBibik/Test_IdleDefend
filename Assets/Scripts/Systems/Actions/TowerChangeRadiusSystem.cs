using System;
using Db;
using Services;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.Actions
{
    public class TowerChangeRadiusSystem : IInitializable, IDisposable
    {
        private readonly TowerView _towerView;
        private readonly TowerBuffRuntimeService _towerBuffRuntimeService;
        private readonly int _maxRange;

        private float _currentRange;

        public TowerChangeRadiusSystem(
            TowerView towerView,
            TowerConfigSettings towerConfigSettings,
            TowerBuffRuntimeService towerBuffRuntimeService
        )
        {
            _towerView = towerView;
            _towerBuffRuntimeService = towerBuffRuntimeService;
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
            UpdateRangeVisual();
        }

        public void UpRadius(float addValue)
        {
            var nextRange = Mathf.Min(_maxRange, _currentRange + addValue);
            var appliedValue = nextRange - _currentRange;

            _currentRange = nextRange;
            _towerView.attackDistance += appliedValue;
            UpdateRangeVisual();
        }

        public void Initialize()
        {
            _currentRange = _towerView.attackDistance;
            _towerBuffRuntimeService.OnChanged += UpdateRangeVisual;
            ChangeRadius(_towerView.attackDistance);
        }

        public void Dispose()
        {
            _towerBuffRuntimeService.OnChanged -= UpdateRangeVisual;
        }

        private void UpdateRangeVisual()
        {
            if (_towerView.Sphere == null)
                return;

            var visualRange = _towerView.attackDistance * _towerBuffRuntimeService.Stats.RangeMultiplier;
            _towerView.Sphere.localScale = new Vector3(visualRange, visualRange, visualRange);
        }
    }
}
