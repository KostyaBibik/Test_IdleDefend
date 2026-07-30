using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;
using Game.Localization;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    public class ShopWindowView : MonoBehaviour
    {
        [SerializeField] private ShopCatalogConfig catalog;
        [SerializeField] private EShopTab defaultTab = EShopTab.Tower;
        [SerializeField] private ShopTabButtonView[] tabButtons;
        [SerializeField] private ShopItemButtonView[] itemViews;
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject windowToShowOnClose;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text selectedItemNameLabel;
        [SerializeField] private TMP_Text selectedItemDescriptionLabel;
        [Tooltip("Попап с живой витриной: открывается по клику игрока на карточку. " +
                 "Не обязателен - без него магазин работает как раньше.")]
        [SerializeField] private TowerPreviewPopupView previewPopup;
        [Tooltip("Попап покупки буста-расходника пачкой: открывается вместо живой витрины " +
                 "для вкладки Boosts.")]
        [SerializeField] private BoostPurchasePopupView boostPurchasePopup;
        [Tooltip("Необязателен: без него окно открывается мгновенно, как раньше.")]
        [SerializeField] private ShopWindowAnimator animator;

        private EShopTab _currentTab;
        private ShopItemDefinition _selectedItem;
        private readonly List<ShopItemButtonView> _visibleItemViews = new();

        // Вкладка перерисовывается и при смене баланса — выкладку карточек играем только когда
        // набор товаров действительно поменялся, иначе сетка дёргалась бы после каждой покупки.
        private bool _itemsLayoutDirty = true;

        private void Awake()
        {
            _currentTab = defaultTab;
            BindTabs();

            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }

        private void OnEnable()
        {
            PlayerSaveData.OnEmeraldsChanged += RefreshCurrentTab;
            ShopInventoryService.OnChanged += RefreshCurrentTab;
            BoostInventoryService.OnChanged += RefreshCurrentTab;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            RefreshStaticLabels();
            _itemsLayoutDirty = true;
            // Строго до ShowTab: вступление глушит прошлые корутины аниматора, а выкладку карточек
            // запускает уже ShowTab.
            animator?.PlayOpen();
            ShowTab(_currentTab);
        }

        private void OnDisable()
        {
            PlayerSaveData.OnEmeraldsChanged -= RefreshCurrentTab;
            ShopInventoryService.OnChanged -= RefreshCurrentTab;
            BoostInventoryService.OnChanged -= RefreshCurrentTab;
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshStaticLabels();
            _itemsLayoutDirty = true;
            animator?.PlayOpen();
            ShowTab(_currentTab);
        }

        private void RefreshStaticLabels()
        {
            titleLabel ??= GameLocalization.FindTextByCurrentValue(this, "Shop", "Магазин");

            if (titleLabel != null)
                titleLabel.text = GameLocalization.Text(LocalizationKey.shop_title, "Shop");
        }

        public void Hide()
        {
            // Витрина держит камеру и RenderTexture — закрываем её вместе с магазином,
            // а не оставляем висеть под скрытым окном.
            if (previewPopup != null)
                previewPopup.Close();

            if (boostPurchasePopup != null)
                boostPurchasePopup.Close();

            gameObject.SetActive(false);

            if (windowToShowOnClose != null)
                windowToShowOnClose.SetActive(true);
        }

        public void ShowTab(EShopTab tab)
        {
            if (previewPopup != null && tab != _currentTab)
                previewPopup.Close();

            if (boostPurchasePopup != null && tab != _currentTab)
                boostPurchasePopup.Close();

            if (tab != _currentTab)
            {
                _itemsLayoutDirty = true;
                PunchTab(tab);
            }

            _currentTab = tab;
            ShopInventoryService.EnsureDefaultEquipped(catalog, tab);
            RefreshTabs();
            RenderItems(catalog != null ? catalog.GetItems(tab).ToList() : new List<ShopItemDefinition>());
        }

        private void RefreshCurrentTab()
        {
            ShowTab(_currentTab);
        }

        private void BindTabs()
        {
            if (tabButtons == null)
                return;

            foreach (var tabButton in tabButtons)
            {
                if (tabButton != null)
                    tabButton.Setup(ShowTab);
            }
        }

        private void RefreshTabs()
        {
            if (tabButtons == null)
                return;

            foreach (var tabButton in tabButtons)
            {
                if (tabButton != null)
                    tabButton.SetSelected(tabButton.Tab == _currentTab);
            }
        }

        private void RenderItems(IReadOnlyList<ShopItemDefinition> items)
        {
            _visibleItemViews.Clear();

            if (itemViews == null)
                return;

            for (var i = 0; i < itemViews.Length; i++)
            {
                var itemView = itemViews[i];
                if (itemView == null)
                    continue;

                if (i >= items.Count)
                {
                    itemView.Setup(null, null, null, null);
                    continue;
                }

                itemView.Setup(items[i], SelectItemByPlayer, BuyItem, EquipItem);
                _visibleItemViews.Add(itemView);
            }

            SelectItem(items.Count > 0 ? items[0] : null);

            if (_itemsLayoutDirty)
            {
                _itemsLayoutDirty = false;
                animator?.PlayItems(_visibleItemViews);
            }
        }

        /// <summary>
        /// Клик игрока по карточке: помимо подписи внизу открывает попап живой витрины.
        /// Отделено от SelectItem, который дёргается и при обычной перерисовке вкладки -
        /// иначе попап открывался бы сам при каждом входе в магазин.
        /// </summary>
        private void SelectItemByPlayer(ShopItemDefinition item)
        {
            SelectItem(item);

            if (item == null)
                return;

            if (item.Tab == EShopTab.Boosts)
            {
                previewPopup?.Close();
                boostPurchasePopup?.Open(item);
                return;
            }

            if (previewPopup == null)
                return;

            if (!CanShowPreview(item))
            {
                previewPopup.Close();
                return;
            }

            previewPopup.Open(item, BuyItem, EquipItem);
        }

        private static bool CanShowPreview(ShopItemDefinition item)
        {
            return item.Tab != EShopTab.Boosts && item.Tab != EShopTab.GemPack;
        }

        private void SelectItem(ShopItemDefinition item)
        {
            _selectedItem = item;

            if (selectedItemNameLabel != null)
                selectedItemNameLabel.text = item != null ? GameLocalization.ShopItemName(item) : string.Empty;

            if (selectedItemDescriptionLabel != null)
                selectedItemDescriptionLabel.text = item != null ? GameLocalization.ShopItemDescription(item) : string.Empty;
        }

        private void BuyItem(ShopItemDefinition item)
        {
            var result = ShopInventoryService.TryPurchase(item);

            if (result.Success && item != null && item.Equippable)
                ShopInventoryService.TryEquip(item);

            RefreshItemViews();

            if (result.Success)
                PunchCardOf(item);
        }

        private void EquipItem(ShopItemDefinition item)
        {
            ShopInventoryService.TryEquip(item);
            RefreshItemViews();
            PunchCardOf(item);
        }

        private void PunchTab(EShopTab tab)
        {
            if (animator == null || tabButtons == null)
                return;

            foreach (var tabButton in tabButtons)
            {
                if (tabButton != null && tabButton.Tab == tab)
                {
                    animator.PunchTab(tabButton);
                    return;
                }
            }
        }

        /// <summary>
        /// Отклик на карточке купленного или надетого товара. Покупка может прийти и из попапа
        /// витрины, поэтому карточку ищем по товару, а не получаем от нажатой кнопки.
        /// </summary>
        private void PunchCardOf(ShopItemDefinition item)
        {
            if (animator == null || item == null)
                return;

            foreach (var itemView in _visibleItemViews)
            {
                if (itemView != null && itemView.Item == item)
                {
                    animator.PunchCard(itemView);
                    return;
                }
            }
        }

        private void RefreshItemViews()
        {
            foreach (var itemView in _visibleItemViews)
                itemView.RefreshState();
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RefreshStaticLabels();
            SelectItem(_selectedItem);
            RefreshItemViews();
        }

        private void OnDestroy()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Hide);
        }
    }
}
