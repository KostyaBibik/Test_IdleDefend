using System.Collections;
using UnityEngine;

namespace Services
{
    public class GameTimeProvider : IGameTimeProvider
    {
        private static readonly float[] GameSpeedModes = { 1f, 2f, 4f };

        public float TimeScale { get; private set; } = 1f;
        public float GameSpeed { get; private set; } = 1f;
        public int GameSpeedModeIndex { get; private set; }
        public float DeltaTime => Time.deltaTime * TimeScale;
        public bool IsPaused => TimeScale <= 0f;

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Mathf.Max(0f, timeScale);
        }

        public void SetGameSpeed(float gameSpeed)
        {
            var modeIndex = GetNearestSpeedModeIndex(gameSpeed);
            GameSpeedModeIndex = modeIndex;
            GameSpeed = GameSpeedModes[modeIndex];

            if (!IsPaused)
                SetTimeScale(GameSpeed);
        }

        public float CycleGameSpeed()
        {
            GameSpeedModeIndex = (GameSpeedModeIndex + 1) % GameSpeedModes.Length;
            GameSpeed = GameSpeedModes[GameSpeedModeIndex];

            if (!IsPaused)
                SetTimeScale(GameSpeed);

            return GameSpeed;
        }

        public void Pause()
        {
            SetTimeScale(0f);
        }

        public void Resume()
        {
            SetTimeScale(GameSpeed);
        }

        public IEnumerator WaitForSeconds(float seconds)
        {
            var elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += DeltaTime;
                yield return null;
            }
        }

        private static int GetNearestSpeedModeIndex(float gameSpeed)
        {
            var nearestIndex = 0;
            var nearestDistance = Mathf.Abs(GameSpeedModes[0] - gameSpeed);

            for (var i = 1; i < GameSpeedModes.Length; i++)
            {
                var distance = Mathf.Abs(GameSpeedModes[i] - gameSpeed);
                if (distance >= nearestDistance)
                    continue;

                nearestIndex = i;
                nearestDistance = distance;
            }

            return nearestIndex;
        }
    }
}
