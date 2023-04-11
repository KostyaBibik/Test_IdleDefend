using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LoadScene
{
    public class LevelLoader : MonoBehaviour
    {
        [SerializeField] private Slider progressSlider;
        
        private void Start()
        {
            StartCoroutine(nameof(LevelLoadSync));
        }

        private IEnumerator LevelLoadSync()
        {
            var loadAsync = SceneManager.LoadSceneAsync("Scenes/GameScene");
            while (loadAsync.progress < 1)
            {
                progressSlider.value = loadAsync.progress;
            }

            yield return new WaitForEndOfFrame();
        }
    }
}