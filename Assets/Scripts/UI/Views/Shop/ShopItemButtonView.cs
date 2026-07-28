using System;
using Db;
using Enums;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    public class ShopItemButtonView : MonoBehaviour
    {
        [SerializeField] private Button selectButton;
        [SerializeField] private Button buyButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text descriptionLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private GameObject ownedState;
        [SerializeField] private GameObject equippedState;
        [SerializeField] private GameObject lockedState;
        [SerializeField] private Transform previewAnchor;

        private ShopItemDefinition _item;
        private GameObject _previewInstance;
        private Action<ShopItemDefinition> _onSelected;
        private Action<ShopItemDefinition> _onBuyRequested;
        private Action<ShopItemDefinition> _onEquipRequested;

        public void Setup(
            ShopItemDefinition item,
            Action<ShopItemDefinition> onSelected,
            Action<ShopItemDefinition> onBuyRequested,
            Action<ShopItemDefinition> onEquipRequested)
        {
            _item = item;
            _onSelected = onSelected;
            _onBuyRequested = onBuyRequested;
            _onEquipRequested = onEquipRequested;

            gameObject.SetActive(item != null);

            if (item == null)
            {
                ClearPreview();
                return;
            }

            if (icon != null)
                icon.sprite = item.Icon;

            if (nameLabel != null)
                nameLabel.text = item.DisplayName;

            if (descriptionLabel != null)
                descriptionLabel.text = item.Description;

            RefreshState();
            RebuildPreview();
            BindButtons();
        }

        public void RefreshState()
        {
            if (_item == null)
                return;

            var state = ShopInventoryService.GetState(_item);

            if (priceLabel != null)
                priceLabel.text = GetPriceText(_item);

            if (statusLabel != null)
                statusLabel.text = state.ToString();

            if (ownedState != null)
                ownedState.SetActive(state == EShopItemState.Owned || state == EShopItemState.Equipped);

            if (equippedState != null)
                equippedState.SetActive(state == EShopItemState.Equipped);

            if (lockedState != null)
                lockedState.SetActive(state == EShopItemState.NotEnoughCurrency);

            if (buyButton != null)
                buyButton.gameObject.SetActive(state == EShopItemState.Available && !ShopInventoryService.IsOwned(_item));

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(_item.Equippable && state == EShopItemState.Owned);
                equipButton.interactable = ShopInventoryService.IsOwned(_item);
            }
        }

        private static string GetPriceText(ShopItemDefinition item)
        {
            return item.PurchaseType switch
            {
                EShopPurchaseType.Free => "Бесплатно",
                EShopPurchaseType.Emeralds => item.EmeraldPrice.ToString(),
                EShopPurchaseType.Iap => item.GemRewardAmount > 0 ? item.GemRewardAmount.ToString("N0") : "IAP",
                _ => string.Empty
            };
        }

        private void BindButtons()
        {
            if (selectButton != null)
            {
                selectButton.onClick.RemoveListener(NotifySelected);
                selectButton.onClick.AddListener(NotifySelected);
            }

            if (buyButton != null)
            {
                buyButton.onClick.RemoveListener(NotifyBuyRequested);
                buyButton.onClick.AddListener(NotifyBuyRequested);
            }

            if (equipButton != null)
            {
                equipButton.onClick.RemoveListener(NotifyEquipRequested);
                equipButton.onClick.AddListener(NotifyEquipRequested);
            }
        }

        private void RebuildPreview()
        {
            ClearPreview();

            if (previewAnchor == null || _item.PreviewPrefab == null)
                return;

            _previewInstance = Instantiate(_item.PreviewPrefab, previewAnchor);
            _previewInstance.transform.localPosition = Vector3.zero;
            _previewInstance.transform.localRotation = Quaternion.identity;

            if (_previewInstance.GetComponent<ShopPreviewRotator>() == null)
                _previewInstance.AddComponent<ShopPreviewRotator>();
        }

        private void ClearPreview()
        {
            if (_previewInstance != null)
                Destroy(_previewInstance);

            _previewInstance = null;
        }

        private void OnDestroy()
        {
            ClearPreview();

            if (selectButton != null)
                selectButton.onClick.RemoveListener(NotifySelected);

            if (buyButton != null)
                buyButton.onClick.RemoveListener(NotifyBuyRequested);

            if (equipButton != null)
                equipButton.onClick.RemoveListener(NotifyEquipRequested);
        }

        private void NotifySelected()
        {
            _onSelected?.Invoke(_item);
        }

        private void NotifyBuyRequested()
        {
            _onBuyRequested?.Invoke(_item);
        }

        private void NotifyEquipRequested()
        {
            _onEquipRequested?.Invoke(_item);
        }
    }
}
