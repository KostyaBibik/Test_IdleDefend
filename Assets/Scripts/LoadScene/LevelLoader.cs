using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoadScene
{
    public class LevelLoader : MonoBehaviour
    {
        private const string TableName = "LocalizationTable";
        private const string LoadingKey = "loading";

        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;

        private void Start()
        {
            StartCoroutine(nameof(LevelLoadSync));
        }

        private IEnumerator LevelLoadSync()
        {
            var loadAsync = SceneManager.LoadSceneAsync("Menu");
            var caption = LoadingCaption();
            var textBuilder = new StringBuilder(caption.Length + 8);

            loadAsync.allowSceneActivation = true;
            while (!loadAsync.isDone)
            {
                var progressValue = loadAsync.progress;
                progressSlider.value = progressValue;
                textBuilder.Clear();
                textBuilder.Append(caption);
                textBuilder.Append(' ');
                textBuilder.Append((int) (progressSlider.value * 100));
                textBuilder.Append('%');
                progressText.text = textBuilder.ToString();
                yield return null;
            }

            yield return new WaitForEndOfFrame();
        }

        private static string LoadingCaption()
        {
            // Локаль выбирает Boot-сцена; если игру запустили в обход неё (редактор) — фолбэк.
            try
            {
                if (!LocalizationSettings.HasSettings || LocalizationSettings.SelectedLocale == null)
                    return "Loading...";

                var value = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, LoadingKey);
                return string.IsNullOrEmpty(value) ? "Loading..." : value;
            }
            catch
            {
                return "Loading...";
            }
        }
    }
}
