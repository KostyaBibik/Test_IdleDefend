using Services;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace UI.Views
{
    /// <summary>
    /// Показывает рекорд выживания (и, опционально, результат последнего забега).
    /// Работает без DI — используется и в Menu, и на LosePanel в GameScene.
    /// </summary>
    public class BestTimeLabel : MonoBehaviour
    {
        private const string TableName = "LocalizationTable";
        private const string BestKey = "best_time";
        private const string RunKey = "run_time";

        [SerializeField] private TMP_Text label;
        [SerializeField] private bool showLastRun;

        private void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (label == null)
                return;

            var best = $"{Localized(BestKey, "Record")}: {SurvivalRecord.Format(SurvivalRecord.BestSeconds)}";

            label.text = showLastRun
                ? $"{Localized(RunKey, "Time")}: {SurvivalRecord.Format(SurvivalRecord.LastRunSeconds)}\n{best}"
                : best;
        }

        private static string Localized(string key, string fallback)
        {
            // В редакторе (запуск в обход Boot-сцены) локаль может быть ещё не выбрана — тогда фолбэк.
            try
            {
                if (!LocalizationSettings.HasSettings || LocalizationSettings.SelectedLocale == null)
                    return fallback;

                var value = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (label == null)
                label = GetComponent<TMP_Text>();
        }
#endif
    }
}
