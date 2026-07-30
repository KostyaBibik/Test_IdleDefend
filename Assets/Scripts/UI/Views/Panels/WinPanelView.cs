using Game.Localization;
using Installers;
using Services;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Panels
{
    public class WinPanelView : MonoBehaviour
    {
        [SerializeField] private Button nextBtn;
        [SerializeField] private Button menuBtn;
        [SerializeField] private GameObject[] starIcons;
        [SerializeField] private TMP_Text titleText;

        [Header("Награда")]
        [Tooltip("Необязателен: без него награда просто не показывается, начисление не меняется.")]
        [SerializeField] private WinPanelAnimator animator;

        private SignalBus _signalBus;
        private bool _transitionStarted;

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
            _signalBus.Subscribe<GameWinSignal>(OnGameWin);
        }

        private void Start()
        {
            nextBtn.onClick.AddListener(delegate
            {
                if (_transitionStarted)
                    return;
                _transitionStarted = true;

                SelectedLevelHolder.SelectedLevelIndex++;

                DiContainerRef.Container.UnbindAll();
                var loader = SceneManager.LoadSceneAsync("GameScene");
                loader.allowSceneActivation = true;
            });

            menuBtn.onClick.AddListener(delegate
            {
                if (_transitionStarted)
                    return;
                _transitionStarted = true;

                var loader = SceneManager.LoadSceneAsync("Menu");
                loader.allowSceneActivation = true;
            });
        }

        private void OnGameWin(GameWinSignal signal)
        {
            RefreshStaticLabels();

            for (var i = 0; i < starIcons.Length; i++)
                starIcons[i].SetActive(i < signal.stars);

            // Сигнал приходит раньше, чем PanelsHandler включит панель, поэтому аниматор только
            // запоминает награду — проигрывает он её из своего OnEnable вместе с остальным вступлением.
            if (animator != null)
                animator.SetReward(signal.reward, signal.unlockedItem);
        }

        private void RefreshStaticLabels()
        {
            titleText ??= GameLocalization.FindTextByCurrentValue(this, "You win!", "Победа!");

            if (titleText != null)
                titleText.text = GameLocalization.Text(LocalizationKey.win_title, "You win!");

            GameLocalization.SetButtonLabel(nextBtn, LocalizationKey.win_next, "Next");
            GameLocalization.SetButtonLabel(menuBtn, LocalizationKey.lose_menu, "Menu");
        }

        private void OnDestroy()
        {
            _signalBus?.Unsubscribe<GameWinSignal>(OnGameWin);
        }
    }
}
