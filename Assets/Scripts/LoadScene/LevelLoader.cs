using System.Collections;
using System.Text;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoadScene
{
    public class LevelLoader : MonoBehaviour
    {
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
            return GameLocalization.Text(LoadingKey, "Loading...");
        }
    }
}
