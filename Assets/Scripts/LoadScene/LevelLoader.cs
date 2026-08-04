using System.Collections;
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

            loadAsync.allowSceneActivation = true;
            while (!loadAsync.isDone)
            {
                progressSlider.value = loadAsync.progress;
                progressText.text = $"{(int) (progressSlider.value * 100)}%";
                yield return null;
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
