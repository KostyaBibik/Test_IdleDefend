using Installers;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Panels
{
    public class PausePanelView : MonoBehaviour
    {
        [SerializeField] private Button continueBtn;
        [SerializeField] private Button restartBtn;
        [SerializeField] private Button giveUpBtn;
        [SerializeField] private TMP_Text levelLabel;

        private IGameTimeProvider _gameTimeProvider;
        private LevelService _levelService;
        private bool _transitionStarted;

        private void Awake()
        {
            continueBtn.onClick.AddListener(Close);

            if (restartBtn != null)
                restartBtn.onClick.AddListener(RestartLevel);

            if (giveUpBtn != null)
                giveUpBtn.onClick.AddListener(GoToMenu);
        }

        public void Open()
        {
            _gameTimeProvider?.Pause();
            RefreshLevelLabel();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _gameTimeProvider?.Resume();
        }

        private void RefreshLevelLabel()
        {
            if (levelLabel == null)
                return;

            // LevelService уже нормализовал индекс, но на всякий случай переживаем его отсутствие.
            var index = _levelService?.CurrentLevelIndex ?? SelectedLevelHolder.SelectedLevelIndex;
            levelLabel.text = $"Уровень {index + 1}";
        }

        private void RestartLevel()
        {
            if (_transitionStarted)
                return;
            _transitionStarted = true;

            // Время живёт в GameTimeProvider, а не в Time.timeScale, но снимаем паузу явно:
            // OnDestroy сработает уже после того, как контейнер будет разобран.
            _gameTimeProvider?.Resume();

            DiContainerRef.Container.UnbindAll();
            var loader = SceneManager.LoadSceneAsync("GameScene");
            loader.allowSceneActivation = true;
        }

        private void GoToMenu()
        {
            if (_transitionStarted)
                return;
            _transitionStarted = true;

            _gameTimeProvider?.Resume();

            var loader = SceneManager.LoadSceneAsync("Menu");
            loader.allowSceneActivation = true;
        }

        [Inject]
        public void Construct(IGameTimeProvider gameTimeProvider, LevelService levelService)
        {
            _gameTimeProvider = gameTimeProvider;
            _levelService = levelService;
        }

        private void OnDestroy()
        {
            continueBtn.onClick.RemoveListener(Close);

            if (restartBtn != null)
                restartBtn.onClick.RemoveListener(RestartLevel);

            if (giveUpBtn != null)
                giveUpBtn.onClick.RemoveListener(GoToMenu);

            if (gameObject.activeSelf)
                _gameTimeProvider?.Resume();
        }
    }
}
