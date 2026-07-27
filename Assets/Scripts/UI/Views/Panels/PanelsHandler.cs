using Signals;
using UnityEngine;
using Zenject;

namespace UI.Views.Panels
{
    public class PanelsHandler : MonoBehaviour
    {
        public GameObject gamePanel;
        public GameObject losePanel;
        public GameObject winPanel;

        private SignalBus _signalBus;

        private void ActivateLosePanel()
        {
            gamePanel.SetActive(false);
            winPanel.SetActive(false);
            losePanel.SetActive(true);
        }

        private void ActivateWinPanel()
        {
            gamePanel.SetActive(false);
            losePanel.SetActive(false);
            winPanel.SetActive(true);
        }

        private void OnLoseGame(GameLoseSignal signal)
        {
            ActivateLosePanel();
        }

        private void OnGameWin(GameWinSignal signal)
        {
            ActivateWinPanel();
        }

        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
            _signalBus.Subscribe<GameLoseSignal>(OnLoseGame);
            _signalBus.Subscribe<GameWinSignal>(OnGameWin);
        }

        private void OnDestroy()
        {
            _signalBus.Unsubscribe<GameLoseSignal>(OnLoseGame);
            _signalBus.Unsubscribe<GameWinSignal>(OnGameWin);
        }
    }
}