using Db;
using Enums;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    /// <summary>
    /// Попап покупки буста-расходника: количество выбирается на месте (+/-), покупка идёт
    /// пачкой через BoostInventoryService.TryPurchase. Открывается по клику на карточку буста
    /// вместо живой витрины TowerPreviewPopupView, которая для Boosts не показывается.
    /// </summary>
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
        [Tooltip("Клик по затемнению за попапом тоже закрывает его")]
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
                nameLabel.text = item.DisplayName;

            if (descriptionLabel != null)
                descriptionLabel.text = item.Description;

            if (unitPriceLabel != null)
                unitPriceLabel.text = $"Цена за 1: {ShopItemPresenter.GetPriceText(item)}";

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
                    ? "Бесплатно"
                    : (_item.EmeraldPrice * _quantity).ToString();
                totalPriceLabel.text = $"Итого: {total}";
            }

            if (minusButton != null)
                minusButton.interactable = _quantity > minQuantity;

            if (plusButton != null)
                plusButton.interactable = _quantity < maxQuantity;
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
