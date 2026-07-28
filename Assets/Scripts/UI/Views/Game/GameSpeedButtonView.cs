using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Game
{
    public class GameSpeedButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        private IGameTimeProvider _gameTimeProvider;

        [Inject]
        public void Construct(IGameTimeProvider gameTimeProvider)
        {
            _gameTimeProvider = gameTimeProvider;
        }

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            UpdateLabel(_gameTimeProvider.GameSpeed);
        }

        private void OnClick()
        {
            var speed = _gameTimeProvider.CycleGameSpeed();
            UpdateLabel(speed);
        }

        private void UpdateLabel(float speed)
        {
            if (label != null)
                label.text = $"x{speed:0}";
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }
    }
}
