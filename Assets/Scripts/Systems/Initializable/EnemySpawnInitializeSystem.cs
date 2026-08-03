using System;
using System.Collections.Generic;
using Db;
using Enums;
using Helpers;
using Infrastructure.Impl;
using Services;
using Systems.RunTime.Camera;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;
using Tutorial;

namespace Systems.Initializable
{
    public class EnemySpawnInitializeSystem : IInitializable, ITickable, IDisposable
    {
        private readonly EntityFactory _entityFactory;
        private readonly LevelService _levelService;
        private readonly SceneHandler _sceneHandler;
        private readonly CameraZoomSystem _cameraZoomSystem;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly TutorialRuntimeState _tutorialRuntime;
        private readonly EnemyPrefabsConfig _enemyPrefabsConfig;

        private readonly List<SpawnEntryState> _entryStates = new();
        private readonly Dictionary<EEnemyType, float> _healthMultiplierByType = new();
        private int _activeSectionIndex = int.MinValue;
        private bool _spawnFinishedNotified;
        private float _elapsedSeconds;

        public EnemySpawnInitializeSystem(
            EntityFactory entityFactory,
            LevelService levelService,
            SceneHandler sceneHandler,
            CameraZoomSystem cameraZoomSystem,
            IGameTimeProvider gameTimeProvider,
            TutorialRuntimeState tutorialRuntime,
            EnemyPrefabsConfig enemyPrefabsConfig
        )
        {
            _entityFactory = entityFactory;
            _levelService = levelService;
            _sceneHandler = sceneHandler;
            _cameraZoomSystem = cameraZoomSystem;
            _gameTimeProvider = gameTimeProvider;
            _tutorialRuntime = tutorialRuntime;
            _enemyPrefabsConfig = enemyPrefabsConfig;
        }

        public void Initialize()
        {
            _entryStates.Clear();
            _healthMultiplierByType.Clear();
            _activeSectionIndex = int.MinValue;
            _spawnFinishedNotified = false;
            _elapsedSeconds = 0f;
        }

        public void Tick()
        {
            if (_spawnFinishedNotified)
                return;

            if (_tutorialRuntime.BlocksStandardGameplay)
                return;

            if (_levelService.CurrentLevel == null)
                return;

            var deltaTime = _gameTimeProvider.DeltaTime;
            if (deltaTime <= 0f)
                return;

            _elapsedSeconds += deltaTime;

            if (_levelService.CurrentLevel.GetSpawnSectionIndex(_elapsedSeconds) < 0)
            {
                NotifySpawnFinished();
                return;
            }

            ActivateCurrentSection();

            for (var i = 0; i < _entryStates.Count; i++)
            {
                var state = _entryStates[i];

                if (state.isStopped)
                    continue;

                if (ShouldStopForLevelEnd(state.entry))
                {
                    state.isStopped = true;
                    _entryStates[i] = state;
                    continue;
                }

                state.remainingDelay -= deltaTime;

                if (state.remainingDelay <= 0f)
                {
                    SpawnEnemy(state.entry);
                    state.remainingDelay = RollDelay(state.entry);
                }

                // SpawnEntryState - структура, значит state здесь копия элемента списка.
                // Записывать её обратно нужно при любом исходе, а не только после спавна:
                // иначе тик отсчёта каждый кадр откатывается и таймер никогда не доходит до нуля.
                _entryStates[i] = state;
            }
        }

        private void ActivateCurrentSection()
        {
            var level = _levelService.CurrentLevel;
            if (level == null)
                return;

            var sectionIndex = level.GetSpawnSectionIndex(_elapsedSeconds);
            if (sectionIndex == _activeSectionIndex)
                return;

            // Волна закончилась - запоминаем, каким HP-множителем она закончилась для каждого
            // типа врага. Следующая волна для того же типа продолжит рост именно с этой отметки,
            // а не сбросится на 1x, иначе на границе волн возникал бы провал/скачок сложности.
            CarryOverHealthMultipliers();

            _activeSectionIndex = sectionIndex;
            _entryStates.Clear();

            if (sectionIndex < 0)
                return;

            var entries = level.GetSpawnEntries(sectionIndex);

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;

                _entryStates.Add(new SpawnEntryState(entry, 0f));
            }
        }

        private void SpawnEnemy(EnemySpawnEntryDefinition entry)
        {
            var safeRadius = _cameraZoomSystem.GetSafeSpawnRadius();
            var distanceFromCenter = Random.Range(safeRadius, safeRadius + 1.5f);
            var randomPoint = _sceneHandler.TowerPos.position +
                              new Vector3(Random.value - 0.5f, Random.value - 0.5f, 0f).normalized *
                              distanceFromCenter;

            // Стартовый множитель этой волны - не всегда 1x: если тот же тип врага уже рос
            // в предыдущей волне, продолжаем с той отметки, на которой он закончил (см. CarryOverHealthMultipliers).
            var startMultiplier = GetCarriedMultiplier(entry.enemyType);
            var targetMultiplier = startMultiplier * entry.endOfWaveHealthMultiplier;
            var healthMultiplier = Mathf.Lerp(startMultiplier, targetMultiplier, GetActiveSectionProgress());

            _entityFactory.CreateEnemy(randomPoint, entry.enemyType, entry.extraHealth, entry.extraSpeed,
                healthMultiplier: healthMultiplier);
        }

        /// <summary>
        /// 0 = только начало текущей волны, 1 = самый её конец. Используется, чтобы
        /// врагам одного типа плавно поднимать HP к концу волны (EnemySpawnEntryDefinition.endOfWaveHealthMultiplier).
        /// </summary>
        private float GetActiveSectionProgress()
        {
            var level = _levelService.CurrentLevel;
            if (level == null)
                return 0f;

            level.GetSpawnSectionBounds(_activeSectionIndex, out var start, out var end);
            var duration = end - start;
            if (duration <= 0f)
                return 0f;

            return Mathf.Clamp01((_elapsedSeconds - start) / duration);
        }

        private float GetCarriedMultiplier(EEnemyType enemyType)
        {
            return _healthMultiplierByType.TryGetValue(enemyType, out var value) ? value : 1f;
        }

        /// <summary>
        /// Вызывается ровно в момент смены волны, пока _entryStates ещё хранит entry-шки
        /// уходящей волны и _activeSectionIndex/_elapsedSeconds ещё её описывают (прогресс = 1).
        /// </summary>
        private void CarryOverHealthMultipliers()
        {
            foreach (var state in _entryStates)
            {
                var entry = state.entry;
                if (entry == null)
                    continue;

                var startMultiplier = GetCarriedMultiplier(entry.enemyType);
                _healthMultiplierByType[entry.enemyType] = startMultiplier * entry.endOfWaveHealthMultiplier;
            }
        }

        private static float RollDelay(EnemySpawnEntryDefinition entry)
        {
            var min = Mathf.Max(0.05f, Mathf.Min(entry.spawnDelayMin, entry.spawnDelayMax));
            var max = Mathf.Max(min, Mathf.Max(entry.spawnDelayMin, entry.spawnDelayMax));
            return Random.Range(min, max);
        }

        private bool ShouldStopForLevelEnd(EnemySpawnEntryDefinition entry)
        {
            var level = _levelService.CurrentLevel;
            if (level == null || _activeSectionIndex != LevelDefinition.MaxStars - 1)
                return false;

            var leadSeconds = entry.finalSpawnLeadSecondsOverride >= 0f
                ? entry.finalSpawnLeadSecondsOverride
                : _enemyPrefabsConfig.GetPrefab(entry.enemyType).FinalSpawnLeadSeconds;

            var remainingSeconds = level.Star3Seconds - _elapsedSeconds;
            return remainingSeconds <= Mathf.Max(0f, leadSeconds);
        }

        private void NotifySpawnFinished()
        {
            _spawnFinishedNotified = true;
            _entryStates.Clear();
            _levelService.NotifyAllWavesSpawned();
        }

        public void Dispose()
        {
            _entryStates.Clear();
        }

        private struct SpawnEntryState
        {
            public readonly EnemySpawnEntryDefinition entry;
            public float remainingDelay;
            public bool isStopped;

            public SpawnEntryState(EnemySpawnEntryDefinition entry, float remainingDelay)
            {
                this.entry = entry;
                this.remainingDelay = remainingDelay;
                isStopped = false;
            }
        }
    }
}
