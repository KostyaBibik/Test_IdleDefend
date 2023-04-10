using Services.Impl;
using Signals;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Enemies
{
    public class EnemyCheckToHitSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;
        private readonly SignalBus _signalBus;
        private const float checkDistance = 0.01f;

        public EnemyCheckToHitSystem(
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
                if(enemy.isDestroyed)
                    continue;
                
                if (CheckOnAttackRange(enemy))
                    break;
            }
        }

        private bool CheckOnAttackRange(EnemyView enemyView)
        {
            var enemyPos = enemyView.transform.position;
            if(Vector3.Distance(_towerView.transform.position, enemyPos) < checkDistance)
            {
               _signalBus.Fire(new TowerLostHealthSignal
               {
                   damageCount = 1
               });
               
               enemyView.healthComponent.DestroyOnAttackTower();
               return true;
            }

            return false;
        }
    }
}