using Infrastructure.Impl;
using Services;
using Services.Impl;
using Systems.RunTime;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.RunTime.Tower
{
    public class TowerAttackSystem : ITickable
    {
        private readonly EnemyService _enemyService;
        private readonly EntityFactory _entityFactory;
        private readonly TowerView _towerView;
        private readonly IGameTimeProvider _gameTimeProvider;

        private float _reloadRemaining;

        public TowerAttackSystem(
            TowerView towerView,
            EnemyService enemyService,
            EntityFactory entityFactory,
            IGameTimeProvider gameTimeProvider
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Tick()
        {
            if (_reloadRemaining > 0f)
            {
                _reloadRemaining -= _gameTimeProvider.DeltaTime;
                return;
            }

            var enemies = _enemyService.GetAssumedActiveEnemies();
            if(enemies.Count <= 0)
                return;

            var nearestEnemy = AttackTargeting.FindNearestEnemy(_towerView.transform.position, enemies);
            if (!CheckOnDistanceAttack(nearestEnemy.transform.position))
                return;

            var bullet = (BulletView) _entityFactory.CreateBullet(_towerView.transform.position);
            bullet.target = nearestEnemy;
            bullet.damage = _towerView.attackDamage;
            nearestEnemy.healthComponent.ReduceAssumedHealth(bullet.damage);

            _reloadRemaining = 1f / _towerView.attackSpeed;
        }

        private bool CheckOnDistanceAttack(Vector3 enemyPos)
        {
            var distance = Vector3.Distance(enemyPos, _towerView.transform.position);
            return distance <= _towerView.attackDistance * _towerView.ratioRange;
        }
    }
}
