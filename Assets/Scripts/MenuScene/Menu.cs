using Db;
using Services;
using UI.Views;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuScene
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private Button exitBtn;
        [SerializeField] private LevelsConfig levelsConfig;
        [SerializeField] private Transform levelsContainer;
        [SerializeField] private LevelButtonView levelButtonPrefab;

        private void Start()
        {
            BuildLevelButtons();
            InitializeExitBtn();
        }

        private void BuildLevelButtons()
        {
            var unlockedIndex = SaveSystem.GetUnlockedLevelIndex();

            for (var i = 0; i < levelsConfig.Count; i++)
            {
                var level = levelsConfig.GetByIndex(i);
                var button = Instantiate(levelButtonPrefab, levelsContainer);

                var unlocked = i <= unlockedIndex;
                var stars = SaveSystem.GetLevelStars(level.LevelId);

                button.Setup(i + 1, unlocked, stars);

                var levelIndex = i;
                button.Button.onClick.AddListener(delegate { PlayLevel(levelIndex); });
            }
        }

        private void PlayLevel(int levelIndex)
        {
            SelectedLevelHolder.SelectedLevelIndex = levelIndex;
            SceneManager.LoadScene("GameScene");
        }

        private void InitializeExitBtn()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // В WebGL Application.Quit() ничего не делает — мёртвая кнопка, модерация Яндекса такое режет.
            exitBtn.gameObject.SetActive(false);
#else
            exitBtn.onClick.AddListener(delegate
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
#endif
        }
    }
}
