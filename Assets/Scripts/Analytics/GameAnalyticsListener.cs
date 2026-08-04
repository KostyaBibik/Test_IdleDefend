using System;
using Services;
using Signals;
using Zenject;

namespace Analytics
{
    /// <summary>
    /// Единственное место, где игра разговаривает с аналитикой: слушает игровые сигналы и переводит
    /// их в события GameAnalytics. Смысл — не дать вызовам GaEventProvider расползтись по геймплейному
    /// коду: открыл этот файл и увидел всю схему событий целиком, а выключается аналитика снятием бинда.
    ///
    /// Биндить ПОСЛЕ LevelService: старт уровня репортится прямо в Initialize(), а к этому моменту
    /// LevelService.Initialize() уже должен разрезолвить CurrentLevel.
    /// </summary>
    public class GameAnalyticsListener : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly LevelService _levelService;

        public GameAnalyticsListener(SignalBus signalBus, LevelService levelService)
        {
            _signalBus = signalBus;
            _levelService = levelService;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<GameWinSignal>(OnGameWin);
            _signalBus.Subscribe<GameLoseSignal>(OnGameLose);

            GaEventProvider.LevelStarted(CurrentLevelId);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<GameWinSignal>(OnGameWin);
            _signalBus.Unsubscribe<GameLoseSignal>(OnGameLose);
        }

        private void OnGameWin(GameWinSignal signal)
        {
            GaEventProvider.LevelCompleted(CurrentLevelId, signal.stars);
        }

        private void OnGameLose(GameLoseSignal signal)
        {
            GaEventProvider.LevelFailed(CurrentLevelId, signal.stars);
        }

        /// <summary>
        /// LevelId, а не порядковый индекс: этим же ключом уровень записывается в сейв
        /// (<see cref="SaveSystem.SaveLevelProgress"/>), поэтому события аналитики и сохранённые
        /// звёзды говорят про один и тот же уровень даже после переупорядочивания списка.
        /// </summary>
        private int CurrentLevelId => _levelService.CurrentLevel != null ? _levelService.CurrentLevel.LevelId : 0;
    }
}
