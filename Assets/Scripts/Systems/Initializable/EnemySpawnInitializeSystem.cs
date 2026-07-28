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

            ActivateCurrentSection();
        }

        public void Tick()
        {
            if (_spawnFinishedNotified)
                return;

            if (_levelService.IsSpawnCapped)
            {
                NotifySpawnFinished();
                return;
            }

            ActivateCurrentSection();

            var deltaTime = _gameTimeProvider.DeltaTime;
            for (var i = 0; i < _entryStates.Count; i++)
            {
                var state = _entryStates[i];
                state.remainingDelay -= deltaTime;

                if (state.remainingDelay > 0f)
                    continue;

                SpawnEnemy(state.entry);
                state.remainingDelay = RollDelay(state.entry);
                _entryStates[i] = state;
            }
        }

        private void ActivateCurrentSection()
        {
            var sectionIndex = _levelService.CurrentLevel.GetSpawnSectionIndex(_levelService.ElapsedSeconds);
            if (sectionIndex == _activeSectionIndex)
                return;

            _activeSectionIndex = sectionIndex;
            _entryStates.Clear();

            var section = _levelService.CurrentLevel.GetSpawnSection(_levelService.ElapsedSeconds);
            var entries = section?.Enemies;
            if (entries == null)
                return;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null)
                    continue;

                _entryStates.Add(new SpawnEntryState(entry, RollDelay(entry)));
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
