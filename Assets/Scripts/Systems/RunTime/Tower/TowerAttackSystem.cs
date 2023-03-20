using System.Collections;
using System.Collections.Generic;
using Infrastructure.Impl;
using Services.Impl;
using UniRx;
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

        private bool _reload;

        public TowerAttackSystem(
            TowerView towerView,
            EnemyService enemyService,
            EntityFactory entityFactory
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _entityFactory = entityFactory;
        }

        public void Tick()
        {
            if (_reload)
                return;

            var enemies = _enemyService.GetAssumedActiveEnemies();
            if(enemies.Count <= 0)
                return;
            
            var nearestEnemy = GetNearestEnemy(enemies.ToArray());
            if (!CheckOnDistanceAttack(nearestEnemy.transform.position))
                return;

            var bullet = (BulletView) _entityFactory.CreateBullet(_towerView.transform.position);
            bullet.target = nearestEnemy;
            bullet.damage = _towerView.attackValue;
            nearestEnemy.healthComponent.ReduceAssumedHealth(bullet.damage);

            Observable.FromCoroutine(Reload).Subscribe();
        }

        private EnemyView GetNearestEnemy(IList<EnemyView> enemies)
        {
            for (var i = 0; i < enemies.Count; i++)
            {
                for (var j = i + 1; j < enemies.Count; j++)
                {
                    if (Vector3.Distance(enemies[i].transform.position, _towerView.transform.position)
                        > Vector3.Distance(enemies[j].transform.position, _towerView.transform.position))
                    {
                        var tempEnemy = enemies[i];
                        enemies[i] = enemies[j];
                        enemies[j] = tempEnemy;
                    }
                }
            }

            return enemies[0];
        }

        private bool CheckOnDistanceAttack(Vector3 enemyPos)
        {
            var distance = Vector3.Distance(enemyPos, _towerView.transform.position);
            return distance <= _towerView.attackDistance * _towerView.ratioRange;
        }

        private IEnumerator Reload()
        {
            _reload = true;

            yield return new WaitForSeconds(1f / _towerView.attackSpeed);

            _reload = false;
        }
    }
}