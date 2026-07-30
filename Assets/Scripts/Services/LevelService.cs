using Db;
using Services.Impl;
using Signals;
using Zenject;
using Tutorial;

namespace Services
{
    public class LevelService : IInitializable, ITickable
    {
        private readonly LevelsConfig _levelsConfig;
        private readonly EnemyService _enemyService;
        private readonly SignalBus _signalBus;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly TutorialRuntimeState _tutorialRuntime;

        private bool _allWavesSpawned;
        private bool _finished;
        private float _elapsed;

        public LevelDefinition CurrentLevel { get; private set; }
        public int CurrentLevelIndex { get; private set; }
        public float ElapsedSeconds => _elapsed;
        public bool IsSpawnCapped => CurrentLevel != null && _elapsed >= CurrentLevel.Star3Seconds;

        public LevelService(
            LevelsConfig levelsConfig,
            EnemyService enemyService,
            SignalBus signalBus,
            IGameTimeProvider gameTimeProvider,
            TutorialRuntimeState tutorialRuntime
        )
        {
            _levelsConfig = levelsConfig;
            _enemyService = enemyService;
            _signalBus = signalBus;
            _gameTimeProvider = gameTimeProvider;
            _tutorialRuntime = tutorialRuntime;
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

            if (_tutorialRuntime.BlocksStandardGameplay)
                return;

            _elapsed += _gameTimeProvider.DeltaTime;

            if (!_allWavesSpawned)
                return;

            if (_enemyService.Enemies.Count > 0)
                return;

            _finished = true;
            _signalBus.Fire<LevelWavesFinishedSignal>();
        }
    }
}
