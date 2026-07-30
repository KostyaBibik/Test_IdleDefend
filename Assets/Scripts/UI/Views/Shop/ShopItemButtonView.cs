using System;
using System.Collections;
using System.Collections.Generic;
using Db;
using Enums;
using Game.Localization;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
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
        [Tooltip("Иконка внутриигровой валюты рядом с ценой. У IAP-товаров цена в реальных деньгах, " +
                 "и валюту показывает priceCurrencyImage из Yandex SDK — поэтому здесь она скрывается, " +
                 "чтобы в строке не оказалось двух разных значков валюты.")]
        [SerializeField] private GameObject priceGemIcon;
        [SerializeField] private RawImage priceCurrencyImage;
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
        private Coroutine _priceCurrencyIconRoutine;

        private static readonly Dictionary<string, Texture2D> PriceCurrencyIconCache = new();

        /// <summary>Товар в карточке. Нужен окну, чтобы найти карточку купленного товара.</summary>
        public ShopItemDefinition Item => _item;

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
                HidePriceCurrencyIcon();
                return;
            }

            if (icon != null)
                icon.sprite = item.Icon;

            if (nameLabel != null)
                nameLabel.text = GameLocalization.ShopItemName(item);

            if (descriptionLabel != null)
                descriptionLabel.text = ShopItemPresenter.GetDescriptionText(item);

            RefreshState();
            RebuildPreview();
            BindButtons();
        }

        public void RefreshState()
        {
            if (_item == null)
                return;

            var state = ShopInventoryService.GetState(_item);
            var isBoost = BoostInventoryService.IsBoostItem(_item);

            if (priceLabel != null)
                priceLabel.text = ShopItemPresenter.GetPriceText(_item);

            // Только IAP: у всех остальных типов покупки поведение ровно как раньше.
            if (priceGemIcon != null)
                priceGemIcon.SetActive(_item.PurchaseType != EShopPurchaseType.Iap);

            if (statusLabel != null)
                statusLabel.text = isBoost ? $"x{BoostInventoryService.GetCount(_item)}" : GameLocalization.ShopState(state);

            if (ownedState != null)
                ownedState.SetActive(!isBoost && (state == EShopItemState.Owned || state == EShopItemState.Equipped));

            if (equippedState != null)
                equippedState.SetActive(state == EShopItemState.Equipped);

            if (lockedState != null)
                lockedState.SetActive(state == EShopItemState.NotEnoughCurrency);

            if (buyButton != null)
                buyButton.gameObject.SetActive(state == EShopItemState.Available && (isBoost || !ShopInventoryService.IsOwned(_item)));

            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(!isBoost && _item.Equippable && state == EShopItemState.Owned);
                equipButton.interactable = ShopInventoryService.IsOwned(_item);
            }

            SetButtonLabel(buyButton, LocalizationKey.shop_buy, "Buy");
            SetButtonLabel(equipButton, LocalizationKey.shop_equip, "Equip");
            RefreshPriceCurrencyIcon();
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
                // Бусты покупаются пачкой в отдельном попапе (см. BoostPurchasePopupView),
                // поэтому их BuyButton просто открывает попап, как и клик по самой карточке.
                buyButton.onClick.RemoveListener(NotifyBuyRequested);
                buyButton.onClick.RemoveListener(NotifySelected);
                buyButton.onClick.AddListener(BoostInventoryService.IsBoostItem(_item) ? NotifySelected : NotifyBuyRequested);
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

            if (previewAnchor != null)
                previewAnchor.gameObject.SetActive(CanShowPreview(_item));

            if (!CanShowPreview(_item) || previewAnchor == null || _item.PreviewPrefab == null)
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

        private void RefreshPriceCurrencyIcon()
        {
            if (priceCurrencyImage == null)
                return;

            StopPriceCurrencyIconRoutine();
            HidePriceCurrencyIcon();

            var url = YandexIapService.GetCatalogCurrencyPictureUrl(_item);
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (PriceCurrencyIconCache.TryGetValue(url, out var cachedTexture) && cachedTexture != null)
            {
                ShowPriceCurrencyIcon(cachedTexture);
                return;
            }

            _priceCurrencyIconRoutine = StartCoroutine(DownloadPriceCurrencyIcon(url, _item));
        }

        private IEnumerator DownloadPriceCurrencyIcon(string url, ShopItemDefinition itemAtRequest)
        {
            using var request = UnityWebRequestTexture.GetTexture(url);
            yield return request.SendWebRequest();

            _priceCurrencyIconRoutine = null;

            if (_item != itemAtRequest)
                yield break;

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                HidePriceCurrencyIcon();
                yield break;
            }

            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            if (texture == null)
            {
                HidePriceCurrencyIcon();
                yield break;
            }

            PriceCurrencyIconCache[url] = texture;
            ShowPriceCurrencyIcon(texture);
        }

        private void ShowPriceCurrencyIcon(Texture texture)
        {
            if (priceCurrencyImage == null)
                return;

            priceCurrencyImage.texture = texture;
            priceCurrencyImage.gameObject.SetActive(true);
        }

        private void HidePriceCurrencyIcon()
        {
            if (priceCurrencyImage == null)
                return;

            priceCurrencyImage.texture = null;
            priceCurrencyImage.gameObject.SetActive(false);
        }

        private void StopPriceCurrencyIconRoutine()
        {
            if (_priceCurrencyIconRoutine == null)
                return;

            StopCoroutine(_priceCurrencyIconRoutine);
            _priceCurrencyIconRoutine = null;
        }

        private static bool CanShowPreview(ShopItemDefinition item)
        {
            return item != null && item.Tab != EShopTab.Boosts && item.Tab != EShopTab.GemPack;
        }

        private static void SetButtonLabel(Button button, LocalizationKey key, string fallback)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = GameLocalization.Text(key, fallback);
        }

        private void OnDestroy()
        {
            StopPriceCurrencyIconRoutine();
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
