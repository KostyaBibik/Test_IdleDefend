using Systems.Actions;
using UnityEngine;
using UnityEngine.UI;
using Views.Impl;
using Zenject;

namespace UI.Views.Game
{
    /// <summary>
    /// Кнопка фирменной ультимативной способности экипированной башни: радиальный кулдаун
    /// + подсветка, когда способность готова. Иконку/готовность берёт из TowerView, которые
    /// TowerInitializeSystem заполняет из экипированного в магазине товара.
    /// </summary>
    public class UltimateButtonView : MonoBehaviour, ITickable
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private Image cooldownFill;
        [SerializeField] private GameObject readyGlow;

        private TowerView _towerView;
        private TowerUltimateSystem _ultimateSystem;
        private bool _hasUltimate;

        [Inject]
        public void Construct(TowerView towerView, TowerUltimateSystem ultimateSystem)
        {
            _towerView = towerView;
            _ultimateSystem = ultimateSystem;
        }

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        private void Start()
        {
            // TowerInitializeSystem уже отработал к моменту первого Tick (Initialize-фаза Zenject
            // идёт раньше Tick-фазы), поэтому здесь можно читать финальные значения TowerView.
            _hasUltimate = _towerView != null && _towerView.ultimateCooldown > 0f;
            gameObject.SetActive(_hasUltimate);

            if (!_hasUltimate)
                return;

            if (icon != null)
                icon.sprite = _towerView.ultimateIcon;

            if (cooldownFill != null)
                cooldownFill.fillAmount = 0f;

            if (readyGlow != null)
                readyGlow.SetActive(true);
        }

        public void Tick()
        {
            if (!_hasUltimate)
                return;

            var ratio = Mathf.Clamp01(_towerView.ultimateCooldownRemaining / _towerView.ultimateCooldown);
            var ready = _towerView.ultimateCooldownRemaining <= 0f;

            if (cooldownFill != null)
                cooldownFill.fillAmount = ratio;

            if (button != null)
                button.interactable = ready;

            if (readyGlow != null)
                readyGlow.SetActive(ready);
        }

        private void OnClick()
        {
            _ultimateSystem?.TryActivate();
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(OnClick);
        }
    }
}
