using System.Collections;
using UnityEngine;

namespace Services
{
    public class GameTimeProvider : IGameTimeProvider
    {
        public float TimeScale { get; private set; } = 1f;
        public float DeltaTime => Time.deltaTime * TimeScale;
        public bool IsPaused => TimeScale <= 0f;

        public void SetTimeScale(float timeScale)
        {
            TimeScale = Mathf.Max(0f, timeScale);
        }

        public void Pause()
        {
            SetTimeScale(0f);
        }

        public void Resume()
        {
            SetTimeScale(1f);
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
    }
}
