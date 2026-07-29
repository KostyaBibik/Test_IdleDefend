using System;
using Enums;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    public class ShopTabButtonView : MonoBehaviour
    {
        [SerializeField] private EShopTab tab;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;
        [Tooltip("Дубликат подписи поверх SelectedState (другой цвет/материал под подсветку выбранной вкладки). " +
                 "Не обязателен — если не назначен, локализуется только обычный label.")]
        [SerializeField] private TMP_Text labelSelected;
        [SerializeField] private GameObject selectedState;

        private Action<EShopTab> _onSelected;

        public EShopTab Tab => tab;

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            RefreshLabel();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        public void Setup(Action<EShopTab> onSelected)
        {
            _onSelected = onSelected;
            RefreshLabel();

            if (button == null)
                return;

            button.onClick.RemoveListener(NotifySelected);
            button.onClick.AddListener(NotifySelected);
        }

        public void SetSelected(bool selected)
        {
            if (selectedState != null)
                selectedState.SetActive(selected);
        }

        private void OnDestroy()
        {
            if (button != null)
                button.onClick.RemoveListener(NotifySelected);
        }

        private void RefreshLabel()
        {
            var text = GameLocalization.ShopTab(tab);

            if (label != null)
                label.text = text;

            if (labelSelected != null)
                labelSelected.text = text;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RefreshLabel();
        }

        private void NotifySelected()
        {
            _onSelected?.Invoke(tab);
        }
    }
}
