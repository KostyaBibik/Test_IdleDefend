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
        private const float splitChildPushOut = 0.5f;
        private const float splitChildSpreadDeg = 40f;
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
        [Inject] private IEntityPoolService _entityPoolService;
        [Inject] private TowerView _towerView;

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
                var rawHealthScale = GetRawHealthScale(enemyDefinition, view);
                var rewardCount = CalculateReward(enemyDefinition, rawHealthScale);

                Enemies.Remove(view);
                var particlePrefab = enemyDefinition.GetRandomParticle();
                var particles = Object.Instantiate(particlePrefab,
                    view.transform.position, Quaternion.identity);
                if (signal.hashReward)
                {
                    if (view.grantCoinReward)
                        _coinService.AddCoins(rewardCount);

                    if (view.grantExperienceReward)
                    {
                        var experienceScale = Mathf.Pow(
                            rawHealthScale, Mathf.Clamp01(_enemyPrefabsConfig.ExperienceHealthScaleExponent));
                        var experience = view.experienceRewardOverride > 0
                            ? view.experienceRewardOverride
                            : Mathf.Max(1,
                                Mathf.CeilToInt(_towerExperienceConfig.GetEnemyExperience(enemyDefinition) * experienceScale));
                        _towerExperienceService.AddExperience(experience, view.transform.position);
                    }

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
                    _entityPoolService.Return(view);
                }
            }
        }

        private IEnumerator SplitAndDestroy(EnemyView view, EnemyDefinition enemyDefinition)
        {
            var deathPos = view.transform.position;

            yield return _gameTimeProvider.WaitForSeconds(enemyDefinition.DeathDelay);

            var childCount = enemyDefinition.SplitChildCount;
            for (var i = 0; i < childCount; i++)
            {
                _entityFactory.CreateEnemy(
                    GetSplitChildPosition(deathPos, i, childCount),
                    enemyDefinition.SplitChildType.Type, 0, 0);
            }

            _entityPoolService.Return(view);
        }

        /// <summary>
        /// Раньше дети спавнились ровно в точке смерти родителя. Родитель обычно умирает уже
        /// внутри зоны поражения, поэтому дети появлялись вплотную к башне и доходили до неё
        /// почти бесплатно — один Splitter стоил игроку больше, чем целая волна. Теперь дети
        /// выталкиваются наружу, минимум на край зоны поражения, и раскладываются веером,
        /// чтобы не слипаться в одну точку.
        /// </summary>
        private Vector3 GetSplitChildPosition(Vector3 deathPos, int index, int count)
        {
            if (_towerView == null)
                return deathPos;

            var towerPos = _towerView.transform.position;
            var offset = deathPos - towerPos;
            var direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.up;

            var effectiveRange = _towerView.attackDistance * _towerView.ratioRange;
            var distance = Mathf.Max(offset.magnitude + splitChildPushOut, effectiveRange * 1.05f);

            // Веер вокруг направления родителя: один ребёнок — строго по нему, несколько — симметрично.
            var spread = count > 1 ? (index / (float) (count - 1) - 0.5f) * splitChildSpreadDeg : 0f;
            var rotated = Quaternion.Euler(0f, 0f, spread) * direction;

            return towerPos + rotated * distance;
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
                if (enemyView != null)
                    _entityPoolService.Return(enemyView);
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

        /// <summary>
        /// Во сколько раз этот конкретный экземпляр врага толще базового из EnemyDefinition.
        /// Уровень раздувает HP через extraHealth (LevelDefinition) и endOfWaveHealthMultiplier
        /// (EnemySpawnInitializeSystem), а награда раньше бралась от базового значения — из-за
        /// этого к 50-му уровню один и тот же грант стоил игроку в 50 раз больше работы за те же
        /// 14 монет, и экономика поздних уровней физически не окупала апгрейды.
        /// Теперь награда идёт пропорционально фактическому HP, то есть "цена" убийства
        /// в монетах остаётся постоянной по всей игре.
        ///
        /// Возвращается СЫРОЙ множитель: монеты и опыт сглаживают его своими экспонентами
        /// (RewardHealthScaleExponent / ExperienceHealthScaleExponent), и эти экспоненты
        /// сильно разные — почему именно, см. комментарий у второй из них.
        /// </summary>
        private static float GetRawHealthScale(EnemyDefinition enemyDefinition, EnemyView view)
        {
            if (enemyDefinition == null || enemyDefinition.Health <= 0)
                return 1f;

            if (view == null || view.healthComponent == null)
                return 1f;

            var actualMaxHealth = view.healthComponent.GetMaxHealth();
            if (actualMaxHealth <= 0)
                return 1f;

            return Mathf.Max(1f, (float) actualMaxHealth / enemyDefinition.Health);
        }

        private int CalculateReward(EnemyDefinition enemyDefinition, float rawHealthScale)
        {
            _activeBoostService.EnsureLoaded();

            // Сглаживание — см. EnemyPrefabsConfig.RewardHealthScaleExponent. Строго
            // пропорциональная награда (экспонента 1) разгоняет экономику вразнос: HP внутри
            // уровня растёт экспоненциально, доход растёт вместе с ним, и к середине боя башня
            // выкупает всё подряд.
            var healthScale = Mathf.Pow(
                rawHealthScale, Mathf.Clamp01(_enemyPrefabsConfig.RewardHealthScaleExponent));

            // Масштаб по HP заходит ДО CalculateEnemyReward, чтобы округление до красивого шага
            // (LevelDefinition.rewardRoundTo) применилось к итоговому числу, а не к базовому.
            var scaledBase = Mathf.Max(1, Mathf.CeilToInt(enemyDefinition.RewardCoins * healthScale));

            var baseReward = _currentLevel != null
                ? _currentLevel.CalculateEnemyReward(scaledBase, _elapsedSeconds)
                : scaledBase;

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
