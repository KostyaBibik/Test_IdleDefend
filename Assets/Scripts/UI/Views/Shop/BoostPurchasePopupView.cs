using Db;
using Enums;
using Game.Localization;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    public class BoostPurchasePopupView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text unitPriceLabel;
        [SerializeField] private TMP_Text quantityLabel;
        [SerializeField] private TMP_Text totalPriceLabel;
        [SerializeField] private Button minusButton;
        [SerializeField] private Button plusButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button backdropButton;
        [SerializeField] private int minQuantity = 1;
        [SerializeField] private int maxQuantity = 99;

        private ShopItemDefinition _item;
        private int _quantity = 1;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.AddListener(Close);

            if (minusButton != null)
                minusButton.onClick.AddListener(DecreaseQuantity);

            if (plusButton != null)
                plusButton.onClick.AddListener(IncreaseQuantity);

            if (buyButton != null)
                buyButton.onClick.AddListener(Buy);

            SetButtonLabel(buyButton, LocalizationKey.shop_buy, "Buy");
        }

        public void Open(ShopItemDefinition item)
        {
            if (item == null)
                return;

            _item = item;
            _quantity = minQuantity;

            gameObject.SetActive(true);

            if (icon != null)
                icon.sprite = item.Icon;

            if (nameLabel != null)
                nameLabel.text = GameLocalization.ShopItemName(item);

            if (descriptionLabel != null)
                descriptionLabel.text = GameLocalization.ShopItemDescription(item);

            if (unitPriceLabel != null)
                unitPriceLabel.text = GameLocalization.Format(
                    LocalizationKey.shop_price_each_format,
                    "Price each: {0}",
                    ShopItemPresenter.GetPriceText(item));

            RefreshQuantity();
        }

        public void Close()
        {
            _item = null;
            gameObject.SetActive(false);
        }

        private void DecreaseQuantity()
        {
            _quantity = Mathf.Max(minQuantity, _quantity - 1);
            RefreshQuantity();
        }

        private void IncreaseQuantity()
        {
            _quantity = Mathf.Min(maxQuantity, _quantity + 1);
            RefreshQuantity();
        }

        private void RefreshQuantity()
        {
            if (quantityLabel != null)
                quantityLabel.text = _quantity.ToString();

            if (totalPriceLabel != null && _item != null)
            {
                var total = _item.PurchaseType == EShopPurchaseType.Free
                    ? GameLocalization.Text(LocalizationKey.shop_free, "Free")
                    : (_item.EmeraldPrice * _quantity).ToString();
                totalPriceLabel.text = GameLocalization.Format(LocalizationKey.shop_total_format, "Total: {0}", total);
            }

            if (minusButton != null)
                minusButton.interactable = _quantity > minQuantity;

            if (plusButton != null)
                plusButton.interactable = _quantity < maxQuantity;
        }

        private static void SetButtonLabel(Button button, LocalizationKey key, string fallback)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = GameLocalization.Text(key, fallback);
        }

        private void Buy()
        {
            if (_item == null)
                return;

            var result = BoostInventoryService.TryPurchase(_item, _quantity);
            if (result.Success)
                Close();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);

            if (backdropButton != null)
                backdropButton.onClick.RemoveListener(Close);

            if (minusButton != null)
                minusButton.onClick.RemoveListener(DecreaseQuantity);

            if (plusButton != null)
                plusButton.onClick.RemoveListener(IncreaseQuantity);

            if (buyButton != null)
                buyButton.onClick.RemoveListener(Buy);
        }
    }
}
