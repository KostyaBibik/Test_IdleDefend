using System.Collections;
using Installers;
using Services;
using Services.Impl;
using Signals;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Panels
{
    public class LosePanelView : MonoBehaviour
    {
        [SerializeField] private PanelsHandler panelsHandler;
        [SerializeField] private Button restartBtn;
        [SerializeField] private Button exitBtn;

        [Header("Продолжение за рекламу")]
        [SerializeField] private Button continueBtn;
        [Tooltip("Вся группа кнопки продолжения — прячется, когда продолжать уже нельзя")]
        [SerializeField] private GameObject continueGroup;
        [Tooltip("Сколько жизней вернуть башне при продолжении")]
        [SerializeField, Min(1)] private int reviveLives = 3;
        [Tooltip("Сколько раз за забег можно продолжить за рекламу")]
        [SerializeField, Min(1)] private int maxContinuesPerRun = 1;

        [Header("Плавный возврат в бой")]
        [Tooltip("С какого таймскейла игра стартует после продолжения")]
        [SerializeField, Range(0.05f, 1f)] private float resumeStartScale = 0.25f;
        [Tooltip("За сколько секунд реального времени таймскейл выходит на 1")]
        [SerializeField, Min(0f)] private float resumeRampSeconds = 1.2f;

        private IGameTimeProvider _gameTimeProvider;
        private EnemyService _enemyService;
        private TowerHealthHandler _towerHealthHandler;
        private SignalBus _signalBus;

        private int _continuesUsed;
        private bool _transitionStarted;
        private bool _restartStarted;
        private bool _continueRequested;

        [Inject]
        public void Construct(
            IGameTimeProvider gameTimeProvider,
            EnemyService enemyService,
            TowerHealthHandler towerHealthHandler,
            SignalBus signalBus)
        {
            _gameTimeProvider = gameTimeProvider;
            _enemyService = enemyService;
            _towerHealthHandler = towerHealthHandler;
            _signalBus = signalBus;
        }

        private void Start()
        {
            restartBtn.onClick.AddListener(OnRestartClicked);
            exitBtn.onClick.AddListener(OnExitClicked);

            if (continueBtn != null)
                continueBtn.onClick.AddListener(OnContinueClicked);
        }

        private void OnEnable()
        {
            // Без остановки времени враги продолжают ползти за панелью, и после продолжения
            // башня умирает в ту же секунду.
            _gameTimeProvider?.Pause();

            _continueRequested = false;

            if (continueBtn != null)
                continueBtn.interactable = true;

            if (continueGroup != null)
                continueGroup.SetActive(_continuesUsed < maxContinuesPerRun);
        }

        // ---------- продолжение за рекламу ----------

        private void OnContinueClicked()
        {
            if (_transitionStarted || _continueRequested || _continuesUsed >= maxContinuesPerRun)
                return;

            _continueRequested = true;
            continueBtn.interactable = false;

            Yandex.Advertisement.ShowReward(
                onRewardedCallback: ContinueGame,
                onErrorCallback: _ => AllowContinueRetry(),
                placement: "lose_continue");
        }

        private void AllowContinueRetry()
        {
            // Реклама не показалась — награду не выдаём, но кнопку возвращаем в рабочее состояние.
            _continueRequested = false;

            if (continueBtn != null)
                continueBtn.interactable = true;
        }

        private void ContinueGame()
        {
            _continuesUsed++;

            _enemyService?.ClearAll();
            RestoreTowerHealth();

            // Панель прячется здесь, поэтому плавный разгон времени запускаем на PanelsHandler:
            // корутина на выключенном объекте не проживёт и кадра.
            panelsHandler.ReturnToGame();
            panelsHandler.StartCoroutine(ResumeSmoothly());
        }

        private void RestoreTowerHealth()
        {
            if (_signalBus == null)
                return;

            for (var i = 0; i < reviveLives; i++)
            {
                if (_towerHealthHandler != null && !_towerHealthHandler.CanUpHealth())
                    break;

                _signalBus.Fire(new TowerAddHealthSignal { additiveCount = 1 });
            }
        }

        private IEnumerator ResumeSmoothly()
        {
            if (_gameTimeProvider == null)
                yield break;

            if (resumeRampSeconds <= 0f)
            {
                _gameTimeProvider.Resume();
                yield break;
            }

            _gameTimeProvider.SetTimeScale(resumeStartScale);

            var elapsed = 0f;
            while (elapsed < resumeRampSeconds)
            {
                // Реальное время: разгоняем как раз тот таймскейл, которым управляем.
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / resumeRampSeconds);
                _gameTimeProvider.SetTimeScale(Mathf.Lerp(resumeStartScale, 1f, t));
                yield return null;
            }

            _gameTimeProvider.Resume();
        }

        // ---------- обычные выходы ----------

        private void OnRestartClicked()
        {
            if (_transitionStarted)
                return;
            _transitionStarted = true;
            restartBtn.interactable = false;

            // Логическая пауза между забегами — штатное место для полноэкранной рекламы.
            // Обёртка сама проверяет NoAds/доступность и зовёт onClose, если показа не было.
            // Ошибка/офлайн тоже должны вести в рестарт, иначе кнопка останется мёртвой.
            Yandex.Advertisement.ShowInterstitial(
                onCloseCallback: RestartGame,
                onErrorCallback: _ => RestartGame(),
                onOfflineCallback: RestartGame,
                placement: "restart");
        }

        private void OnExitClicked()
        {
            if (_transitionStarted)
                return;
            _transitionStarted = true;

            _gameTimeProvider?.Resume();

            var loader = SceneManager.LoadSceneAsync("Menu");
            loader.allowSceneActivation = true;
        }

        private void RestartGame()
        {
            if (_restartStarted)
                return;
            _restartStarted = true;

            _gameTimeProvider?.Resume();

            DiContainerRef.Container.UnbindAll();
            var loader = SceneManager.LoadSceneAsync("GameScene");
            loader.allowSceneActivation = true;
        }

        private void OnDestroy()
        {
            restartBtn.onClick.RemoveListener(OnRestartClicked);
            exitBtn.onClick.RemoveListener(OnExitClicked);

            if (continueBtn != null)
                continueBtn.onClick.RemoveListener(OnContinueClicked);
        }
    }
}
