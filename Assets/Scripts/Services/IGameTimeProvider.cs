using System.Collections;

namespace Services
{
    public interface IGameTimeProvider
    {
        float TimeScale { get; }
        float DeltaTime { get; }
        bool IsPaused { get; }

        void SetTimeScale(float timeScale);
        void Pause();
        void Resume();
        IEnumerator WaitForSeconds(float seconds);
    }
}
