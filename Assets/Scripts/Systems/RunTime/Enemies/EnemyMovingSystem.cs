using Db;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime
{
    public class EnemyMovingSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly TowerView _towerView;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;

        private const float speedRotating = 2f;
        
        public EnemyMovingSystem(
            EnemyService enemyService,
            TowerView towerView,
            EnemyPrefabsConfig prefabsConfig
        )
        {
            _enemyService = enemyService;
            _towerView = towerView;
            _enemyPrefabsConfig = prefabsConfig;
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
            var speedMoving = _enemyPrefabsConfig.SpeedMoving;
            
            enemy.transform.position = Vector3.MoveTowards(
                enemyPos, 
                towerPos,
                Time.deltaTime * speedMoving);
            
            var angle = Mathf.Atan2(towerPos.y - enemyPos.y, towerPos.x - enemyPos.x ) * Mathf.Rad2Deg;
            var targetRotation = Quaternion.Euler(new Vector3(0, 0, angle));
            enemy.Mesh.rotation = Quaternion.RotateTowards(enemy.Mesh.rotation, targetRotation, speedRotating * Time.deltaTime);
           
        }
    }
}