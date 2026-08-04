using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DefaultNamespace.Yandex
{
    public class BootLoadingScreen : MonoBehaviour
    {
        private const string PrefabResourcePath = "BootLoadingScreen";

        [SerializeField] private Slider progressSlider;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private float progressSmoothSpeed = 1.2f;

        private float _displayedProgress;
        private float _targetProgress;

        public static BootLoadingScreen Instance { get; private set; }

        public static BootLoadingScreen Show()
        {
            if (Instance != null)
                return Instance;

            var prefab = Resources.Load<GameObject>(PrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"BootLoadingScreen prefab not found at Resources/{PrefabResourcePath}");
                return null;
            }

            var instance = Instantiate(prefab).GetComponentInChildren<BootLoadingScreen>();
            DontDestroyOnLoad(instance.gameObject);
            Instance = instance;
            Instance.ResetProgress();
            return Instance;
        }

        private void Update()
        {
            if (Mathf.Approximately(_displayedProgress, _targetProgress))
                return;

            _displayedProgress = Mathf.MoveTowards(_displayedProgress, _targetProgress,
                progressSmoothSpeed * Time.unscaledDeltaTime);
            ApplyProgress(_displayedProgress);
        }

        private void ResetProgress()
        {
            _displayedProgress = 0f;
            _targetProgress = 0f;
            ApplyProgress(0f);
        }

        // Sets the target the bar animates towards; the displayed value never jumps, it eases towards it in Update.
        public void SetProgress(float value)
        {
            _targetProgress = Mathf.Clamp01(value);
        }

        private void ApplyProgress(float value)
        {
            if (progressSlider != null)
                progressSlider.value = value;

            if (progressText != null)
                progressText.text = $"{(int) (value * 100)}%";
        }

        public IEnumerator LoadTargetScene(string sceneName, float progressFrom)
        {
            var loadAsync = SceneManager.LoadSceneAsync(sceneName);
            loadAsync.allowSceneActivation = true;

            while (!loadAsync.isDone)
            {
                var realProgress = Mathf.Clamp01(loadAsync.progress / 0.9f);
                SetProgress(Mathf.Lerp(progressFrom, 1f, realProgress));
                yield return null;
            }

            SetProgress(1f);

            // Keep the loading screen alive until the bar has visually caught up to 100%,
            // so it never disappears on a value the player didn't see reached.
            while (_displayedProgress < 1f)
                yield return null;

            yield return new WaitForEndOfFrame();
            Instance = null;
            Destroy(gameObject);
        }
    }
}
