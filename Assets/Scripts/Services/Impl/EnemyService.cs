using System;
using System.Collections.Generic;
using System.Linq;
using Db;
using Signals;
using UnityEngine;
using Views;
using Views.Impl;
using Zenject;
using Object = UnityEngine.Object;

namespace Services.Impl
{
    public class EnemyService : IEntityService, IInitializable, IDisposable
    {
        private const float delayBeforeClearParticle = 1.5f;
        
        private readonly List<EnemyView> enemies = new List<EnemyView>();
        private readonly CoinService _coinService;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly SignalBus _signalBus;
        
        public EnemyService(
            CoinService coinService,
            EnemyPrefabsConfig enemyPrefabsConfig,
            SignalBus signalBus
            )
        {
            _coinService = coinService;
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _signalBus = signalBus;
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

        public void RemoveEntityFromService(DestroyEntitySignal signal)
        {
            var view = (EnemyView)signal.view;
            if (enemies.Contains(view))
            {
                var enemyPrefab = _enemyPrefabsConfig.GetPrefab(view.type);
                var rewardCount = enemyPrefab.rewardKillCoins;
                
                enemies.Remove(view);
                var particles = Object.Instantiate(enemyPrefab.killEnemyParticles,
                    view.transform.position, Quaternion.identity);
                if (signal.hashReward)
                {
                    _coinService.AddCoins(rewardCount);
                    
                    _signalBus.Fire(new ShowRewardSignal
                    {
                        worldPos = view.transform.position,
                        count = rewardCount
                    });
                }
                
                Object.Destroy(particles, delayBeforeClearParticle);
                Object.Destroy(view.gameObject);
            }
        }

        public void Initialize()
        {
            _signalBus.Subscribe<DestroyEntitySignal>(RemoveEntityFromService);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<DestroyEntitySignal>(RemoveEntityFromService);
        }
    }
}