using Services;
using UnityEngine;
using Views;
using Zenject;

namespace Systems.RunTime
{
    public class EnemyMovingSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;

        public EnemyMovingSystem(
            EnemyService enemyService,
            TowerView towerView
        )
        {
            _enemyService = enemyService;
            _towerView = towerView;
        }

        public void Tick()
        {
            foreach (var enemy in _enemyService.Enemies)
            {
                MoveToTower(enemy);
            }
        }

        private void MoveToTower(EnemyView enemy)
        {
            var enemyPos = enemy.transform.position;
            var towerPos = _towerView.transform.position;
            var direction = towerPos - enemyPos;
            enemy.transform.position = Vector3.MoveTowards(
                enemyPos, 
                towerPos,
                Time.deltaTime * 2f);
            enemy.Mesh.transform.LookAt(direction);
        }
    }
}