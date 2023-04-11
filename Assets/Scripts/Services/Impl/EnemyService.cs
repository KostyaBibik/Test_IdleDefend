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

        public List<EnemyView> Enemies { get; } = new();

        public void AddEntityOnService(IEntityView entityView)
        {
            Enemies.Add((EnemyView) entityView);
        }

        public void RemoveEntityFromService(DestroyEntitySignal signal)
        {
            var view = (EnemyView) signal.view;
            if (Enemies.Contains(view))
            {
                var enemyPrefab = _enemyPrefabsConfig.GetPrefab(view.type);
                var rewardCount = enemyPrefab.rewardKillCoins;

                Enemies.Remove(view);
                var particlePrefab = enemyPrefab.GetRandomParticle();
                var particles = Object.Instantiate(particlePrefab,
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

                Object.Destroy(particles.gameObject, delayBeforeClearParticle);
                Object.Destroy(view.gameObject);
            }
        }

        public List<EnemyView> GetAssumedActiveEnemies()
        {
            return Enemies.Where(enemyView => enemyView.healthComponent.CheckAssumedStatus()).ToList();
        }
        
        private void RemoveAllEnemies()
        {
            foreach (var enemyView in Enemies)
            {
                if(enemyView.gameObject != null)
                    Object.Destroy(enemyView.gameObject);
            }
            
            Enemies.Clear(); 
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<DestroyEntitySignal>(RemoveEntityFromService);
        }
        
        public void Dispose()
        {
            RemoveAllEnemies();
            
            _signalBus.Unsubscribe<DestroyEntitySignal>(RemoveEntityFromService);
        }
    }
}