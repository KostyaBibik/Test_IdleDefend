using System.Collections.Generic;
using System.Linq;
using Db;
using UnityEngine;
using Views;
using Views.Impl;

namespace Services.Impl
{
    public class EnemyService : IEntityService
    {
        private const float delayBeforeClearParticle = 1.5f;
        
        private readonly List<EnemyView> enemies = new List<EnemyView>();
        private readonly CoinService _coinService;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;

        public EnemyService(
            CoinService coinService,
            EnemyPrefabsConfig enemyPrefabsConfig
            )
        {
            _coinService = coinService;
            _enemyPrefabsConfig = enemyPrefabsConfig;
        }
        
        public List<EnemyView> Enemies => enemies;
        
        public List<EnemyView> GetAssumedActiveEnemies()
        {
            return enemies.Where(enemyView => enemyView.healthComponent.CheckAssumedStatus()).ToList();
        }
        
        public void AddEntityOnService(IEntityView entityView)
        {
            enemies.Add((EnemyView)entityView);
        }

        public void RemoveEntityFromService(IEntityView entityView)
        {
            var view = (EnemyView) entityView;
            if (enemies.Contains(view))
            {
                _coinService.AddCoins(_enemyPrefabsConfig.RewardKillCoins);
                enemies.Remove(view);
                var particles = Object.Instantiate(_enemyPrefabsConfig.KillEnemyParticles.gameObject,
                    view.transform.position, Quaternion.identity);
                Object.Destroy(particles, delayBeforeClearParticle);
                Object.Destroy(view.gameObject);
            }
        }
    }
}