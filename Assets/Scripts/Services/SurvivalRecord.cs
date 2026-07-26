using System;

namespace Services
{
    /// <summary>
    /// Результат забега: время последней попытки + рекорд из облачного сейва.
    /// Статикой, чтобы UI на неактивных панелях (LosePanel) и в других сценах (Menu)
    /// читал его без DI — данные живут ровно один запуск игры, кроме рекорда.
    /// </summary>
    public static class SurvivalRecord
    {
        public static float LastRunSeconds { get; set; }

        public static float BestSeconds => SaveSystem.SaveData.BestSurvivalTime;

        /// <summary>Возвращает true, если результат забега стал новым рекордом.</summary>
        public static bool SubmitRun(float seconds)
        {
            LastRunSeconds = seconds;

            if (seconds <= SaveSystem.SaveData.BestSurvivalTime)
                return false;

            SaveSystem.SaveData.BestSurvivalTime = seconds;
            SaveSystem.Instance.SaveToStorage();
            return true;
        }

        public static string Format(float seconds)
        {
            if (seconds < 0f)
                seconds = 0f;

            var time = TimeSpan.FromSeconds(seconds);
            return $"{(int) time.TotalMinutes:00}:{time.Seconds:00}";
        }
    }
}
