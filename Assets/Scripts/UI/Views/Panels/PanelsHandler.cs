using Signals;
using UnityEngine;
using Zenject;

namespace UI.Views.Panels
{
    public class PanelsHandler : MonoBehaviour
    {
        public GameObject gamePanel;
        public GameObject losePanel;

        private SignalBus _signalBus;

        private void ActivateLosePanel()
        {
            gamePanel.SetActive(false);
            losePanel.SetActive(true);
        }

        private void OnLoseGame(GameLoseSignal signal)
        {
            ActivateLosePanel();
        }
        
        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
            _signalBus.Subscribe<GameLoseSignal>(OnLoseGame);
        }
        
        private void OnDestroy()
        {
            _signalBus.Unsubscribe<GameLoseSignal>(OnLoseGame);
        }
    }
}