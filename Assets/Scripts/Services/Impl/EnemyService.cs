using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;
using Infrastructure.Impl;
using Signals;
using UniRx;
using UnityEngine;
using Views;
using Views.Impl;
using Zenject;
using Object = UnityEngine.Object;

namespace Services.Impl
{
    public class EnemyService : IEntityService, IInitializable, ITickable, IDisposable
    {
        private const float delayBeforeClearParticle = 1.5f;
        private readonly CoinService _coinService;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;
        private readonly LevelsConfig _levelsConfig;
        private readonly SignalBus _signalBus;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly ActiveBoostService _activeBoostService;
        private readonly TowerBuffRuntimeService _towerBuffRuntimeService;
        private readonly TowerExperienceService _towerExperienceService;
        private readonly TowerExperienceConfig _towerExperienceConfig;
        private LevelDefinition _currentLevel;
        private float _elapsedSeconds;

        [Inject] private EntityFactory _entityFactory;

        public EnemyService(
            CoinService coinService,
            EnemyPrefabsConfig enemyPrefabsConfig,
            LevelsConfig levelsConfig,
            SignalBus signalBus,
            IGameTimeProvider gameTimeProvider,
            ActiveBoostService activeBoostService,
            TowerBuffRuntimeService towerBuffRuntimeService,
            TowerExperienceService towerExperienceService,
            TowerExperienceConfig towerExperienceConfig
        )
        {
            _coinService = coinService;
            _enemyPrefabsConfig = enemyPrefabsConfig;
            _levelsConfig = levelsConfig;
            _signalBus = signalBus;
            _gameTimeProvider = gameTimeProvider;
            _activeBoostService = activeBoostService;
            _towerBuffRuntimeService = towerBuffRuntimeService;
            _towerExperienceService = towerExperienceService;
            _towerExperienceConfig = towerExperienceConfig;
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
                var enemyDefinition = _enemyPrefabsConfig.GetPrefab(view.type);
                var rewardCount = CalculateReward(enemyDefinition);

                Enemies.Remove(view);
                var particlePrefab = enemyDefinition.GetRandomParticle();
                var particles = Object.Instantiate(particlePrefab,
                    view.transform.position, Quaternion.identity);
                if (signal.hashReward)
                {
                    _coinService.AddCoins(rewardCount);
                    _towerExperienceService.AddExperience(_towerExperienceConfig.GetEnemyExperience(enemyDefinition));

                    // Всплывающая монета больше не показывается - над врагами теперь живут числа
                    // урона (ShowDamageNumbersSystem). Начисление наград и опыта выше не изменилось.
                }

                Object.Destroy(particles.gameObject, delayBeforeClearParticle);

                if (enemyDefinition.OnDeath == EEnemyDeathBehavior.SplitIntoChildren)
                {
                    view.PlayDeathAnimation();
                    Observable.FromCoroutine(() => SplitAndDestroy(view, enemyDefinition)).Subscribe();
                }
                else
                {
                    Object.Destroy(view.gameObject);
                }
            }
        }

        private IEnumerator SplitAndDestroy(EnemyView view, EnemyDefinition enemyDefinition)
        {
            var deathPos = view.transform.position;

            yield return _gameTimeProvider.WaitForSeconds(enemyDefinition.DeathDelay);

            for (var i = 0; i < enemyDefinition.SplitChildCount; i++)
            {
                _entityFactory.CreateEnemy(deathPos, enemyDefinition.SplitChildType.Type, 0, 0);
            }

            Object.Destroy(view.gameObject);
        }

        public List<EnemyView> GetAssumedActiveEnemies()
        {
            return Enemies.Where(enemyView => enemyView.healthComponent.CheckAssumedStatus()).ToList();
        }
        
        /// <summary>
        /// Убирает всех живых врагов без наград и партиклов — используется при продолжении
        /// игры за рекламу, чтобы башня не умерла повторно в ту же секунду.
        /// </summary>
        public void ClearAll()
        {
            RemoveAllEnemies();
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
            var levelIndex = SelectedLevelHolder.SelectedLevelIndex;
            if (levelIndex < 0 || levelIndex >= _levelsConfig.Count)
                levelIndex = 0;

            _currentLevel = _levelsConfig.GetByIndex(levelIndex);

            _signalBus.Subscribe<DestroyEntitySignal>(RemoveEntityFromService);
        }

        public void Tick()
        {
            _elapsedSeconds += _gameTimeProvider.DeltaTime;
        }

        private int CalculateReward(EnemyDefinition enemyDefinition)
        {
            _activeBoostService.EnsureLoaded();

            var baseReward = _currentLevel != null
                ? _currentLevel.CalculateEnemyReward(enemyDefinition.RewardCoins, _elapsedSeconds)
                : enemyDefinition.RewardCoins;

            return Mathf.CeilToInt(baseReward
                                   * _activeBoostService.CoinRewardMultiplier
                                   * _towerBuffRuntimeService.Stats.CoinRewardMultiplier);
        }
        
        public void Dispose()
        {
            RemoveAllEnemies();
            
            _signalBus.Unsubscribe<DestroyEntitySignal>(RemoveEntityFromService);
        }
    }
}
