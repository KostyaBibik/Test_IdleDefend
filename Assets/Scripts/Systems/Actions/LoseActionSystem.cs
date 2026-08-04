using System;
using Db;
using Services;
using Signals;
using Zenject;

namespace Systems.Actions
{
    /// <summary>
    /// На поражении фиксирует звёзды за прожитое время и выдаёт награду за их прирост.
    ///
    /// Проигранный забег тоже чего-то стоит: пороги звёзд — те же, что рисует прогресс-бар боя,
    /// поэтому игрок весь уровень видит, сколько уже заработал. Если результата хватило хотя бы
    /// на 1 звезду, уровень считается пройденным и открывает следующий по карте.
    /// </summary>
    public class LoseActionSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly LevelService _levelService;
        private readonly LevelsConfig _levelsConfig;
        private bool _lose;

        public LoseActionSystem(SignalBus signalBus, LevelService levelService, LevelsConfig levelsConfig)
        {
            _signalBus = signalBus;
            _levelService = levelService;
            _levelsConfig = levelsConfig;
        }

        private void OnLoseGame(GameLoseSignal loseSignal)
        {
            if (_lose)
                return;

            _lose = true;

            var level = _levelService.CurrentLevel;
            if (level == null)
                return;

            var stars = level.GetStarsForElapsed(_levelService.ElapsedSeconds);

            // Награда считается до сохранения: платят за прирост относительно лучшего результата.
            var reward = LevelRewardService.GrantForRun(level, stars);
            loseSignal.stars = stars;
            loseSignal.reward = reward;

            if (stars > 0)
            {
                var nextIndex = Math.Min(_levelService.CurrentLevelIndex + 1, _levelsConfig.Count - 1);
                SaveSystem.SaveLevelProgress(level.LevelId, stars, nextIndex);
            }
        }

        public void Initialize()
        {
            _signalBus.Subscribe<GameLoseSignal>(OnLoseGame);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<GameLoseSignal>(OnLoseGame);
        }
    }
}
