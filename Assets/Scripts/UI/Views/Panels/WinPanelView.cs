using Installers;
using Services;
using Signals;
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
            for (var i = 0; i < starIcons.Length; i++)
                starIcons[i].SetActive(i < signal.stars);
        }

        private void OnDestroy()
        {
            _signalBus?.Unsubscribe<GameWinSignal>(OnGameWin);
        }
    }
}
