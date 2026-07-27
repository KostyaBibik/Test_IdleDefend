using Infrastructure.Impl;
using Services.Impl;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.SideTower
{
    public class SideTowerAttackSystem : ITickable
    {
        private readonly SideTowerService _sideTowerService;
        private readonly EnemyService _enemyService;
        private readonly EntityFactory _entityFactory;

        public SideTowerAttackSystem(
            SideTowerService sideTowerService,
            EnemyService enemyService,
            EntityFactory entityFactory
        )
        {
            _sideTowerService = sideTowerService;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
        }

        public void Tick()
        {
            foreach (var tower in _sideTowerService.Towers)
            {
                Attack(tower);
            }
        }

        private void Attack(SideTowerView tower)
        {
            if (tower.reloadRemaining > 0f)
            {
                tower.reloadRemaining -= Time.deltaTime;
                return;
            }

            var enemies = _enemyService.GetAssumedActiveEnemies();
            if (enemies.Count <= 0)
                return;

            var towerPos = tower.transform.position;
            var nearestEnemy = AttackTargeting.FindNearestEnemy(towerPos, enemies);

            if (Vector3.Distance(towerPos, nearestEnemy.transform.position) > tower.attackDistance)
                return;

            var bullet = (BulletView) _entityFactory.CreateBullet(towerPos);
            bullet.target = nearestEnemy;
            bullet.damage = tower.attackDamage;
            nearestEnemy.healthComponent.ReduceAssumedHealth(bullet.damage);

            tower.reloadRemaining = 1f / tower.attackSpeed;
        }
    }
}
