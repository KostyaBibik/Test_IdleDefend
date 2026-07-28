using System;
using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;
using Services;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    /// <summary>
    /// Предбоевой экран выбора бустов-расходников. Открывается перед загрузкой GameScene;
    /// сама сцена не грузится, пока игрок не подтвердит - реальная загрузка идёт через
    /// callback, переданный в Open.
    /// </summary>
    public class BoostSelectWindowView : MonoBehaviour
    {
        [SerializeField] private ShopCatalogConfig catalog;
        [SerializeField] private BoostSelectItemView[] itemViews;
        [SerializeField] private Button startButton;
        [SerializeField] private Button closeButton;

        private Action<int> _onStartRequested;
        private int _pendingLevelIndex;

        private void Awake()
        {
            if (startButton != null)
                startButton.onClick.AddListener(NotifyStart);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnEnable()
        {
            BoostInventoryService.OnChanged += RefreshItems;
        }

        private void OnDisable()
        {
            BoostInventoryService.OnChanged -= RefreshItems;
        }

        public void Open(int levelIndex, Action<int> onStartRequested)
        {
            _pendingLevelIndex = levelIndex;
            _onStartRequested = onStartRequested;

            gameObject.SetActive(true);
            RefreshItems();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        private void RefreshItems()
        {
            if (itemViews == null)
                return;

            var boosts = catalog != null ? catalog.GetItems(EShopTab.Boosts).ToList() : new List<ShopItemDefinition>();

            for (var i = 0; i < itemViews.Length; i++)
            {
                var itemView = itemViews[i];
                if (itemView == null)
                    continue;

                itemView.Setup(i < boosts.Count ? boosts[i] : null);
            }
        }

        private void NotifyStart()
        {
            var levelIndex = _pendingLevelIndex;
            Close();
            _onStartRequested?.Invoke(levelIndex);
        }

        private void OnDestroy()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(NotifyStart);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }
    }
}
