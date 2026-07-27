using Helpers;
using Services;
using UI.Views.Game;
using UnityEngine;
using Zenject;

namespace Systems.RunTime.UI
{
    public class LevelTimeProgressBarSystem : IInitializable, ITickable
    {
        private readonly LevelService _levelService;
        private readonly SceneHandler _sceneHandler;
        private readonly bool[] _starReached = new bool[3];

        public LevelTimeProgressBarSystem(
            LevelService levelService,
            SceneHandler sceneHandler
        )
        {
            _levelService = levelService;
            _sceneHandler = sceneHandler;
        }

        public void Initialize()
        {
            var view = _sceneHandler.LevelTimeProgressBarView;
            if (view == null)
                return;

            var level = _levelService.CurrentLevel;
            view.PositionStars(level.Star1Seconds, level.Star2Seconds, level.Star3Seconds);
            view.SetFillRatio(0f);
        }

        public void Tick()
        {
            var view = _sceneHandler.LevelTimeProgressBarView;
            if (view == null)
                return;

            var level = _levelService.CurrentLevel;
            var elapsed = _levelService.ElapsedSeconds;

            view.SetFillRatio(level.Star3Seconds > 0f ? elapsed / level.Star3Seconds : 0f);

            TryReachStar(view, 0, level.Star1Seconds, elapsed);
            TryReachStar(view, 1, level.Star2Seconds, elapsed);
            TryReachStar(view, 2, level.Star3Seconds, elapsed);
        }

        private void TryReachStar(LevelTimeProgressBarView view, int starIndex, float thresholdSeconds, float elapsed)
        {
            if (_starReached[starIndex] || elapsed < thresholdSeconds)
                return;

            _starReached[starIndex] = true;
            view.PlayStarReached(starIndex);
        }
    }
}
