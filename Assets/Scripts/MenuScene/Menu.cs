using Db;
using Services;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuScene
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private Button exitBtn;
        [SerializeField] private LevelsConfig levelsConfig;
        [SerializeField] private LevelPathBuilder levelPathBuilder;
        [Header("Windows")]
        [SerializeField] private GameObject mainWindow;
        [SerializeField] private GameObject stageWindow;
        [SerializeField] private GameObject shopWindow;
        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button stageButton;

        private void Start()
        {
            InitializeNavigationButtons();
            levelPathBuilder.Build(levelsConfig, PlayLevel);
            InitializeExitBtn();
            HideWindows();
        }

        private void PlayLevel(int levelIndex)
        {
            SelectedLevelHolder.SelectedLevelIndex = levelIndex;
            SceneManager.LoadScene("GameScene");
        }

        private void InitializeNavigationButtons()
        {
            if (shopButton != null)
                shopButton.onClick.AddListener(ShowShop);

            if (stageButton != null)
                stageButton.onClick.AddListener(ShowStage);
        }

        private void ShowShop()
        {
            if (mainWindow != null)
                mainWindow.SetActive(false);

            if (stageWindow != null)
                stageWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(true);
        }

        private void ShowStage()
        {
            if (mainWindow != null)
                mainWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(false);

            if (stageWindow != null)
                stageWindow.SetActive(true);
        }

        private void HideWindows()
        {
            if (stageWindow != null)
                stageWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(false);
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

        private void OnDestroy()
        {
            if (shopButton != null)
                shopButton.onClick.RemoveListener(ShowShop);

            if (stageButton != null)
                stageButton.onClick.RemoveListener(ShowStage);
        }
    }
}
