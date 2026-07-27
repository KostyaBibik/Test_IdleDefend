using Db;
using Services.Impl;
using Signals;
using UnityEngine;
using Zenject;

namespace Services
{
    public class LevelService : IInitializable, ITickable
    {
        private readonly LevelsConfig _levelsConfig;
        private readonly EnemyService _enemyService;
        private readonly SignalBus _signalBus;

        private bool _allWavesSpawned;
        private bool _finished;
        private float _elapsed;

        public LevelDefinition CurrentLevel { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public float ElapsedSeconds => _elapsed;
        public bool IsSpawnCapped => _elapsed >= CurrentLevel.Star3Seconds;

        public LevelService(
            LevelsConfig levelsConfig,
            EnemyService enemyService,
            SignalBus signalBus
        )
        {
            _levelsConfig = levelsConfig;
            _enemyService = enemyService;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            CurrentLevelIndex = SelectedLevelHolder.SelectedLevelIndex;
            if (CurrentLevelIndex < 0 || CurrentLevelIndex >= _levelsConfig.Count)
                CurrentLevelIndex = 0;

            CurrentLevel = _levelsConfig.GetByIndex(CurrentLevelIndex);
        }

        public void NotifyAllWavesSpawned()
        {
            _allWavesSpawned = true;
        }

        public void Tick()
        {
            if (_finished)
                return;

            _elapsed += Time.deltaTime;

            if (!_allWavesSpawned)
                return;

            if (_enemyService.Enemies.Count > 0)
                return;

            _finished = true;
            _signalBus.Fire<LevelWavesFinishedSignal>();
        }
    }
}
