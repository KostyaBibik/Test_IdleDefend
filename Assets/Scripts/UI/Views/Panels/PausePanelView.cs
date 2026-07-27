using Services;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Panels
{
    public class PausePanelView : MonoBehaviour
    {
        [SerializeField] private Button continueBtn;

        private IGameTimeProvider _gameTimeProvider;

        private void Awake()
        {
            continueBtn.onClick.AddListener(Close);
        }

        public void Open()
        {
            _gameTimeProvider?.Pause();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            _gameTimeProvider?.Resume();
        }

        [Inject]
        public void Construct(IGameTimeProvider gameTimeProvider)
        {
            _gameTimeProvider = gameTimeProvider;
        }

        private void OnDestroy()
        {
            continueBtn.onClick.RemoveListener(Close);

            if (gameObject.activeSelf)
                _gameTimeProvider?.Resume();
        }
    }
}
