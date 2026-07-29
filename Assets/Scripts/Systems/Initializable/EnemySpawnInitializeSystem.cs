using System;
using System.Collections.Generic;
using Db;
using Helpers;
using Infrastructure.Impl;
using Services;
using Systems.RunTime.Camera;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Systems.Initializable
{
    public class EnemySpawnInitializeSystem : IInitializable, ITickable, IDisposable
    {
        private readonly EntityFactory _entityFactory;
        private readonly LevelService _levelService;
        private readonly SceneHandler _sceneHandler;
        private readonly CameraZoomSystem _cameraZoomSystem;
        private readonly IGameTimeProvider _gameTimeProvider;

        private readonly List<SpawnEntryState> _entryStates = new();
        private int _activeSectionIndex = int.MinValue;
        private bool _spawnFinishedNotified;
        private float _elapsedSeconds;

        public EnemySpawnInitializeSystem(
            EntityFactory entityFactory,
            LevelService levelService,
            SceneHandler sceneHandler,
            CameraZoomSystem cameraZoomSystem,
            IGameTimeProvider gameTimeProvider
        )
        {
            _entityFactory = entityFactory;
            _levelService = levelService;
            _sceneHandler = sceneHandler;
            _cameraZoomSystem = cameraZoomSystem;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Initialize()
        {
            _entryStates.Clear();
            _activeSectionIndex = int.MinValue;
            _spawnFinishedNotified = false;
            _elapsedSeconds = 0f;
        }

        public void Tick()
        {
            if (_spawnFinishedNotified)
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

            _activeSectionIndex = sectionIndex;
            _entryStates.Clear();

            if (sectionIndex < 0)
                return;

            var section = level.GetSpawnSection(_elapsedSeconds);
            var entries = section?.Enemies;
            if (entries == null)
                return;

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

            _entityFactory.CreateEnemy(randomPoint, entry.enemyType, entry.extraHealth, entry.extraSpeed);
        }

        private static float RollDelay(EnemySpawnEntryDefinition entry)
        {
            var min = Mathf.Max(0.05f, Mathf.Min(entry.spawnDelayMin, entry.spawnDelayMax));
            var max = Mathf.Max(min, Mathf.Max(entry.spawnDelayMin, entry.spawnDelayMax));
            return Random.Range(min, max);
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

            public SpawnEntryState(EnemySpawnEntryDefinition entry, float remainingDelay)
            {
                this.entry = entry;
                this.remainingDelay = remainingDelay;
            }
        }
    }
}
