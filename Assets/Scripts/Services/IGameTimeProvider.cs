using System.Collections;

namespace Services
{
    public interface IGameTimeProvider
    {
        float TimeScale { get; }
        float GameSpeed { get; }
        int GameSpeedModeIndex { get; }
        float DeltaTime { get; }
        bool IsPaused { get; }

        void SetTimeScale(float timeScale);
        void SetGameSpeed(float gameSpeed);
        float CycleGameSpeed();
        void Pause();
        void Resume();
        IEnumerator WaitForSeconds(float seconds);
    }
}
