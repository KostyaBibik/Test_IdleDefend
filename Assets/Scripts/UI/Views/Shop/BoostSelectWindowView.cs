using System;
using System.Collections.Generic;
using System.Linq;
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
        [SerializeField] private TMP_Text titleLabel;
        [Tooltip("Необязателен: без него окно открывается и закрывается мгновенно, как раньше.")]
        [SerializeField] private BoostSelectWindowAnimator animator;

        private Action<int> _onStartRequested;
        private int _pendingLevelIndex;

        // Окно можно успеть открыть заново, пока играет финал закрытия: счётчик даёт отложенному
        // закрытию понять, что оно устарело.
        private int _openGeneration;

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
            _openGeneration++;

            gameObject.SetActive(true);
            RefreshStaticLabels();
            RefreshItems();

            // Строго после RefreshItems: вступление считает набор видимых строк, а его определяет
            // именно эта перерисовка.
            animator?.PlayIntro();
        }

        private void RefreshStaticLabels()
        {
            titleLabel ??= GameLocalization.FindTextByCurrentValue(this, "Select boosts", "Выбор бустов");

            if (titleLabel != null)
                titleLabel.text = GameLocalization.Text(LocalizationKey.boost_select_title, "Select boosts");

            GameLocalization.SetButtonLabel(startButton, LocalizationKey.menu_play, "Play");
        }

        public void Close()
        {
            if (animator == null || !gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                return;
            }

            var generation = _openGeneration;
            animator.PlayOutro(() =>
            {
                if (generation == _openGeneration)
                    gameObject.SetActive(false);
            });
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

                itemView.Toggled -= OnItemToggled;
                itemView.Toggled += OnItemToggled;
                itemView.Setup(i < boosts.Count ? boosts[i] : null);
            }
        }

        private void OnItemToggled(BoostSelectItemView itemView)
        {
            animator?.PunchRow(itemView);
        }

        private void NotifyStart()
        {
            var levelIndex = _pendingLevelIndex;

            // Здесь закрываемся без анимации: следом грузится GameScene, и доигрывать финал
            // всё равно негде.
            _openGeneration++;
            gameObject.SetActive(false);

            _onStartRequested?.Invoke(levelIndex);
        }

        private void OnDestroy()
        {
            if (itemViews != null)
            {
                foreach (var itemView in itemViews)
                    if (itemView != null)
                        itemView.Toggled -= OnItemToggled;
            }

            if (startButton != null)
                startButton.onClick.RemoveListener(NotifyStart);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }
    }
}
