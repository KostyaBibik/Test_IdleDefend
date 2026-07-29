using System.Collections;
using Services;
using Signals;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Panels
{
    public class PanelsHandler : MonoBehaviour
    {
        public GameObject gamePanel;
        public GameObject losePanel;
        public GameObject winPanel;
        public PausePanelView pausePanel;
        public Button pauseButton;

        [SerializeField, Min(0f)] private float losePanelDelaySeconds = 2f;
        [SerializeField, Min(0f)] private float losePanelFadeSeconds = 0.35f;

        private SignalBus _signalBus;
        private IGameTimeProvider _gameTimeProvider;
        private Coroutine _losePanelRoutine;

        private void Start()
        {
            pauseButton.onClick.AddListener(pausePanel.Open);
        }

        /// <summary>
        /// Возврат в бой после продолжения за рекламу.
        /// </summary>
        public void ReturnToGame()
        {
            StopLosePanelRoutine();
            losePanel.SetActive(false);
            winPanel.SetActive(false);
            gamePanel.SetActive(true);
        }

        private void ActivateLosePanel()
        {
            gamePanel.SetActive(false);
            winPanel.SetActive(false);
            losePanel.SetActive(true);
        }

        private void ActivateWinPanel()
        {
            StopLosePanelRoutine();
            gamePanel.SetActive(false);
            losePanel.SetActive(false);
            winPanel.SetActive(true);
        }

        private void OnLoseGame(GameLoseSignal signal)
        {
            _gameTimeProvider?.Pause();
            StopLosePanelRoutine();
            _losePanelRoutine = StartCoroutine(ShowLosePanelDelayed());
        }

        private void OnGameWin(GameWinSignal signal)
        {
            ActivateWinPanel();
        }

        [Inject]
        public void Construct(SignalBus signalBus, IGameTimeProvider gameTimeProvider)
        {
            _signalBus = signalBus;
            _gameTimeProvider = gameTimeProvider;
            _signalBus.Subscribe<GameLoseSignal>(OnLoseGame);
            _signalBus.Subscribe<GameWinSignal>(OnGameWin);
        }

        private IEnumerator ShowLosePanelDelayed()
        {
            var canvasGroup = GetLoseCanvasGroup();
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (losePanelDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(losePanelDelaySeconds);

            ActivateLosePanel();

            if (losePanelFadeSeconds <= 0f)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
                _losePanelRoutine = null;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < losePanelFadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / losePanelFadeSeconds);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            _losePanelRoutine = null;
        }

        private CanvasGroup GetLoseCanvasGroup()
        {
            var canvasGroup = losePanel.GetComponent<CanvasGroup>();
            return canvasGroup != null ? canvasGroup : losePanel.AddComponent<CanvasGroup>();
        }

        private void StopLosePanelRoutine()
        {
            if (_losePanelRoutine == null)
                return;

            StopCoroutine(_losePanelRoutine);
            _losePanelRoutine = null;
        }

        private void OnDestroy()
        {
            StopLosePanelRoutine();

            _signalBus.Unsubscribe<GameLoseSignal>(OnLoseGame);
            _signalBus.Unsubscribe<GameWinSignal>(OnGameWin);
        }
    }
}
