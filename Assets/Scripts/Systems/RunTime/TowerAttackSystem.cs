using System.Collections;
using Services;
using UniRx;
using UnityEngine;
using Views;
using Zenject;

namespace Systems.RunTime
{
    public class TowerAttackSystem : ITickable
    {
        private readonly TowerView _towerView;
        private readonly EnemyService _enemyService;

        private bool _reload;
        
        public TowerAttackSystem(
            TowerView towerView,
            EnemyService enemyService
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
        }
        
        public void Tick()
        {
            if(_reload)
                return;
            
            foreach (var enemy in _enemyService.Enemies)
            {
                if (CheckOnDistanceAttack(enemy.transform.position))
                {
                    Debug.Log("enemy.healthComponent.ReduceHealth(10);");
                    enemy.healthComponent.ReduceHealth(10);
                    Observable.FromCoroutine(Reload)
                        .Subscribe();
                    return;
                }
            }
        }

        private bool CheckOnDistanceAttack(Vector3 enemyPos)
        {
            var distance = Vector3.Distance(enemyPos, _towerView.transform.position);
            return distance <= _towerView.attackDistance;
        }

        private IEnumerator Reload()
        {
            Debug.Log("Reload" + 1f / _towerView.attackSpeed);
            _reload = true;
            
            yield return new WaitForSeconds(1f / _towerView.attackSpeed);

            Debug.Log("end Reload");
            _reload = false;
        }
    }
}