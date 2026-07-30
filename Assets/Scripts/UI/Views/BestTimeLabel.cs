using Services;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace UI.Views
{
    /// <summary>
    /// Показывает результат последнего забега на LosePanel.
    /// Старые экземпляры компонента в Menu и StagesWindow отключаются автоматически.
    /// </summary>
    public class BestTimeLabel : MonoBehaviour
    {
        private const string TableName = "LocalizationTable";
        private const string RunKey = "run_time";

        [SerializeField] private TMP_Text label;
        [SerializeField] private bool showLastRun;

        private void Awake()
        {
            if (!showLastRun)
                gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (showLastRun)
                Refresh();
        }

        public void Refresh()
        {
            if (!showLastRun)
            {
                gameObject.SetActive(false);
                return;
            }

            if (label == null)
                return;

            label.text = $"{Localized(RunKey, "Time")}: {SurvivalRecord.Format(SurvivalRecord.LastRunSeconds)}";
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
