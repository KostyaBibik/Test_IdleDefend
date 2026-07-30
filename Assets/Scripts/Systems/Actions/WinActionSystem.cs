using System;
using Db;
using Services;
using Signals;
using Zenject;

namespace Systems.Actions
{
    /// <summary>
    /// На завершении волн уровня считает звёзды по времени выживания,
    /// сохраняет лучший результат и открывает следующий уровень.
    /// </summary>
    public class WinActionSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly LevelService _levelService;
        private readonly LevelsConfig _levelsConfig;
        private readonly TowerExperienceService _experienceService;
        private readonly TowerBuffSelectionService _buffSelectionService;

        private bool _finished;

        public WinActionSystem(
            SignalBus signalBus,
            LevelService levelService,
            LevelsConfig levelsConfig,
            TowerExperienceService experienceService,
            TowerBuffSelectionService buffSelectionService
        )
        {
            _signalBus = signalBus;
            _levelService = levelService;
            _levelsConfig = levelsConfig;
            _experienceService = experienceService;
            _buffSelectionService = buffSelectionService;
        }

        private void OnLevelWavesFinished(LevelWavesFinishedSignal signal)
        {
            if (_finished)
                return;

            _finished = true;
            _buffSelectionService.CancelForLevelEnd();
            _experienceService.FinishLevel();

            // Этот сигнал стреляет только когда все волны уже заспавнены и врагов не осталось —
            // то есть уровень зачищен полностью. Другого исхода у этого пути нет, поэтому 3 звезды безусловно.
            const int stars = LevelDefinition.MaxStars;

            // Награду считаем до сохранения: она платится за прирост звёзд, а SaveLevelProgress
            // уже запишет новый результат как лучший.
            var reward = LevelRewardService.GrantForRun(_levelService.CurrentLevel, stars);
            var unlockedItem = LevelRewardService.GrantUnlockItem(_levelService.CurrentLevel);

            SaveProgress(stars);

            _signalBus.Fire(new GameWinSignal
            {
                stars = stars,
                reward = reward,
                unlockedItem = unlockedItem,
            });
        }

        private void SaveProgress(int stars)
        {
            var levelId = _levelService.CurrentLevel.LevelId;
            var nextIndex = Math.Min(_levelService.CurrentLevelIndex + 1, _levelsConfig.Count - 1);
            SaveSystem.SaveLevelProgress(levelId, stars, nextIndex);
        }

        public void Initialize()
        {
            _signalBus.Subscribe<LevelWavesFinishedSignal>(OnLevelWavesFinished);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<LevelWavesFinishedSignal>(OnLevelWavesFinished);
        }
    }
}
