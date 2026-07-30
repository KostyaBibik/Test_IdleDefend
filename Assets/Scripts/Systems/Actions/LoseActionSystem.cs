using System;
using Services;
using Signals;
using Zenject;

namespace Systems.Actions
{
    /// <summary>
    /// На поражении фиксирует звёзды за прожитое время и выдаёт награду за их прирост.
    ///
    /// Проигранный забег тоже чего-то стоит: пороги звёзд — те же, что рисует прогресс-бар боя,
    /// поэтому игрок весь уровень видит, сколько уже заработал. Следующий уровень при этом
    /// не открывается — карта двигается только победой.
    /// </summary>
    public class LoseActionSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly LevelService _levelService;
        private bool _lose;

        public LoseActionSystem(SignalBus signalBus, LevelService levelService)
        {
            _signalBus = signalBus;
            _levelService = levelService;
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
                SaveSystem.SaveLevelStars(level.LevelId, stars);
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
