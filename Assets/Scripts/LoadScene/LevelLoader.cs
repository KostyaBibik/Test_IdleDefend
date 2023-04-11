using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoadScene
{
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        
        private void Start()
        {
            StartCoroutine(nameof(LevelLoadSync));
        }

        private IEnumerator LevelLoadSync()
        {
            var loadAsync = SceneManager.LoadSceneAsync("Menu");
            var textBuilder = new StringBuilder
            {
                Capacity = 18
            };

            loadAsync.allowSceneActivation = true;
            while (!loadAsync.isDone)
            {
                var progressValue = loadAsync.progress;
                progressSlider.value = progressValue;
                textBuilder.Clear();
                textBuilder.Append("Loading...");
                textBuilder.Insert(10, (int) (progressSlider.value * 100));
                textBuilder.Append("%");
                progressText.text = textBuilder.ToString();
                yield return null;
            }

            yield return new WaitForEndOfFrame();
        }
    }
}