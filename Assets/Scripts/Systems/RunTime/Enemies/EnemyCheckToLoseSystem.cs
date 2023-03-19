using Services.Impl;
using Signals;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Enemies
{
    public class EnemyCheckToLoseSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;
        private readonly SignalBus _signalBus;
        private const float checkDistance = 0.01f;

        public EnemyCheckToLoseSystem(
            EnemyService enemyService,
            TowerView towerView,
            SignalBus signalBus
        )
        {
            _enemyService = enemyService;
            _towerView = towerView;
            _signalBus = signalBus;
        }

        public void Tick()
        {
            foreach (var enemy in _enemyService.Enemies)
            {
                CheckDistanceToTower(enemy.transform.position);
            }
        }

        private void CheckDistanceToTower(Vector3 enemyPos)
        {
            if(Vector3.Distance(_towerView.transform.position, enemyPos) < checkDistance)
            {
                _signalBus.Fire<GameLoseSignal>();
            }
        }
    }
}