using System;
using Db;
using Enums;
using Game.Localization;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    /// <summary>
    /// Попап с живой витриной башни: открывается по клику на карточку магазина, показывает
    /// имитацию боя и дублирует кнопки покупки/экипировки - то есть является ещё и точкой
    /// конверсии, а не только смотрелкой. Сетка карточек при этом не трогается.
    /// </summary>
    public class TowerPreviewPopupView : MonoBehaviour
    {
        [SerializeField] private TowerPreviewSurfaceView previewSurface;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button closeButton;
        [Tooltip("Клик по затемнению за попапом тоже закрывает его")]
        [SerializeField] private Button backdropButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private GameObject equippedState;
        [SerializeField] private GameObject priceRoot;

        private ShopItemDefinition _item;
        private Action<ShopItemDefinition> _onBuyRequested;
        private Action<ShopItemDefinition> _onEquipRequested;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);

            if (buyButton != null)
                buyButton.onClick.AddListener(NotifyBuyRequested);

            if (equipButton != null)
                equipButton.onClick.AddListener(NotifyEquipRequested);
        }

        public void Open(
            ShopItemDefinition item,
            Action<ShopItemDefinition> onBuyRequested,
            Action<ShopItemDefinition> onEquipRequested)
        {
            if (item == null)
                return;

            _item = item;
            _onBuyRequested = onBuyRequested;
            _onEquipRequested = onEquipRequested;

            gameObject.SetActive(true);

            if (nameLabel != null)
                nameLabel.text = GameLocalization.ShopItemName(item);

            if (descriptionLabel != null)
                descriptionLabel.text = GameLocalization.ShopItemDescription(item);

            if (previewSurface != null)
                previewSurface.Show(item);

            RefreshState();
        }

        public void Close()
        {
            if (previewSurface != null)
                previewSurface.Hide();

            _item = null;
            gameObject.SetActive(false);
        }

        public void RefreshState()
        {
            if (_item == null)
                return;

            var state = ShopInventoryService.GetState(_item);
            var owned = ShopInventoryService.IsOwned(_item);

            if (priceLabel != null)
                priceLabel.text = ShopItemPresenter.GetPriceText(_item);

            if (priceRoot != null)
                priceRoot.SetActive(!owned);

            if (buyButton != null)
                buyButton.gameObject.SetActive(!owned);

            if (equipButton != null)
                equipButton.gameObject.SetActive(_item.Equippable && state == EShopItemState.Owned);

            if (equippedState != null)
                equippedState.SetActive(state == EShopItemState.Equipped);

            SetButtonLabel(buyButton, LocalizationKey.shop_buy, "Buy");
            SetButtonLabel(equipButton, LocalizationKey.shop_equip, "Equip");
        }

        private static void SetButtonLabel(Button button, LocalizationKey key, string fallback)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = GameLocalization.Text(key, fallback);
        }

        private void NotifyBuyRequested()
        {
            var item = _item;
            _onBuyRequested?.Invoke(item);
            RefreshState();
        }

        private void NotifyEquipRequested()
        {
            var item = _item;
            _onEquipRequested?.Invoke(item);
            RefreshState();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.RemoveListener(Close);

            if (buyButton != null)
                buyButton.onClick.RemoveListener(NotifyBuyRequested);

            if (equipButton != null)
                equipButton.onClick.RemoveListener(NotifyEquipRequested);
        }
    }
}
