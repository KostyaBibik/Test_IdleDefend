using Game.Localization;
using Services;
using TMPro;
using UnityEngine;

namespace UI.Views
{
    /// <summary>
    /// Показывает результат последнего забега на LosePanel.
    /// Старые экземпляры компонента в Menu и StagesWindow отключаются автоматически.
    /// </summary>
    public class BestTimeLabel : MonoBehaviour
    {
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
            return GameLocalization.Text(key, fallback);
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
