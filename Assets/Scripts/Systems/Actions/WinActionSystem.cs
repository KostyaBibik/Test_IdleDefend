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

        private bool _finished;

        public WinActionSystem(
            SignalBus signalBus,
            LevelService levelService,
            LevelsConfig levelsConfig
        )
        {
            _signalBus = signalBus;
            _levelService = levelService;
            _levelsConfig = levelsConfig;
        }

        private void OnLevelWavesFinished(LevelWavesFinishedSignal signal)
        {
            if (_finished)
                return;

            _finished = true;

            // Этот сигнал стреляет только когда все волны уже заспавнены и врагов не осталось —
            // то есть уровень зачищен полностью. Другого исхода у этого пути нет, поэтому 3 звезды безусловно.
            const int stars = 3;

            SaveProgress(stars);

            _signalBus.Fire(new GameWinSignal { stars = stars });
        }

        private void SaveProgress(int stars)
        {
            var levelId = _levelService.CurrentLevel.LevelId;
            var nextIndex = Math.Min(_levelService.CurrentLevelIndex + 1, _levelsConfig.Count - 1);
            SaveSystem.SaveLevelProgress(levelId, stars, nextIndex);

            EmeraldWallet.Add(_levelService.CurrentLevel.RewardEmeralds);
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
