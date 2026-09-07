using System.Collections;
using DefaultNamespace.Yandex;
using Db;
using Enums;
using Game.Localization;
using Services;
using UI;
using UI.Views.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuScene
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private Button exitBtn;
        [SerializeField] private LevelsConfig levelsConfig;
        [SerializeField] private LevelPathBuilder levelPathBuilder;
        [SerializeField] private ShopCatalogConfig shopCatalogConfig;
        [SerializeField] private Transform towerPreviewAnchor;
        [SerializeField] private float towerPreviewScale = 2f;
        [Tooltip("Живая витрина башни на главном экране. Не назначена — показывается статичный PreviewPrefab товара.")]
        [SerializeField] private TowerPreviewSurfaceView towerPreviewSurfacePrefab;
        [Header("Title")]
        [SerializeField] private TMP_Text titleLine1;
        [SerializeField] private TMP_Text titleLine2;
        [Header("Windows")]
        [SerializeField] private GameObject mainWindow;
        [SerializeField] private GameObject stageWindow;
        [SerializeField] private GameObject shopWindow;
        [SerializeField] private BoostSelectWindowView boostSelectWindow;
        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button stageButton;
        [SerializeField] private Button stageBackButton;

        private IEnumerator Start()
        {
            // On the first application launch the string table may still be loading when Menu.Start runs.
            // Synchronous localization calls then return their English fallbacks, while subsequent visits work
            // because the table is already cached. Warm up the table before creating any menu labels or nodes.
            yield return LocalizationSettings.InitializationOperation;
            if (!GameLocalization.IsEmptyLocale)
            {
                var localizationWarmup = LocalizationSettings.StringDatabase.GetLocalizedStringAsync(
                    "LocalizationTable", LocalizationKey.menu_shop.ToString());
                yield return localizationWarmup;
            }

            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            InitializeNavigationButtons();
            RefreshStaticLabels();
            levelPathBuilder.Build(levelsConfig, OpenBoostSelect);
            InitializeExitBtn();
            HideWindows();

            yield return new WaitForEndOfFrame();
            GameReady.Notify();
            global::Yandex.Advertisement.ShowInterstitial(placement: "startup");
        }

        private void RefreshStaticLabels()
        {
            if (titleLine1 != null)
                titleLine1.text = GameLocalization.Text(LocalizationKey.menu_title_line_1, "Spacechpok");

            if (titleLine2 != null)
                titleLine2.text = GameLocalization.Text(LocalizationKey.menu_title_line_2, string.Empty);

            GameLocalization.SetButtonLabel(shopButton, LocalizationKey.menu_shop, "Shop");
            GameLocalization.SetButtonLabel(stageButton, LocalizationKey.menu_play, "Play");
            GameLocalization.SetButtonLabel(stageBackButton, LocalizationKey.lose_menu, "Menu");

            if (stageWindow != null)
                GameLocalization.SetTextByCurrentValue(stageWindow.transform, LocalizationKey.menu_stages, "Stages", "Stages", "Этапы");
        }

        /// <summary>
        /// Клик по уровню на карте открывает выбор бустов на бой вместо немедленной загрузки
        /// GameScene. Если экран не назначен, ведём себя как раньше и грузим уровень сразу.
        /// </summary>
        private void OnSelectedLocaleChanged(Locale _)
        {
            RefreshStaticLabels();
        }

        private void OpenBoostSelect(int levelIndex)
        {
            if (boostSelectWindow == null)
            {
                PlayLevel(levelIndex);
                return;
            }

            // Определение уровня передаём сюда, а не даём окну свою ссылку на LevelsConfig:
            // окно и так открывается только отсюда, а лишняя привязка в префабе — лишний способ
            // забыть её проставить.
            var level = levelIndex >= 0 && levelIndex < levelsConfig.Count
                ? levelsConfig.GetByIndex(levelIndex)
                : null;

            boostSelectWindow.Open(levelIndex, level, PlayLevel);
        }

        /// <summary>
        /// Показывает экипированную башню на главном экране. Если назначена живая витрина
        /// (та же, что в попапе магазина) - башня стоит и отстреливается по подлетающим манекенам;
        /// иначе откатываемся на статичный PreviewPrefab из карточки магазина.
        /// </summary>
        private void SetupTowerPreview()
        {
            if (towerPreviewAnchor == null || shopCatalogConfig == null)
                return;

            ShopInventoryService.EnsureDefaultEquipped(shopCatalogConfig, EShopTab.Tower);
            var equippedItem = ShopInventoryService.GetEquippedItem(shopCatalogConfig, EShopTab.Tower);
            if (equippedItem == null)
                return;

            if (towerPreviewSurfacePrefab != null)
            {
                var surface = Instantiate(towerPreviewSurfacePrefab, towerPreviewAnchor);
                surface.transform.localPosition = Vector3.zero;
                surface.transform.localScale = Vector3.one;
                surface.Show(equippedItem, transparentBackground: true);
                return;
            }

            if (equippedItem.PreviewPrefab == null)
                return;

            var instance = Instantiate(equippedItem.PreviewPrefab, towerPreviewAnchor);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * towerPreviewScale;
        }

        private void PlayLevel(int levelIndex)
        {
            SelectedLevelHolder.SelectedLevelIndex = levelIndex;
            SceneManager.LoadScene("GameScene");
        }

        private void InitializeNavigationButtons()
        {
            if (shopButton != null)
                shopButton.onClick.AddListener(ShowShop);

            if (stageButton != null)
                stageButton.onClick.AddListener(ShowStage);

            if (stageBackButton != null)
                stageBackButton.onClick.AddListener(ShowMain);
        }

        private void ShowMain()
        {
            if (stageWindow != null)
                stageWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(false);

            if (mainWindow != null)
                mainWindow.SetActive(true);
        }

        private void ShowShop()
        {
            if (mainWindow != null)
                mainWindow.SetActive(false);

            if (stageWindow != null)
                stageWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(true);
        }

        private void ShowStage()
        {
            if (mainWindow != null)
                mainWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(false);

            if (stageWindow != null)
                stageWindow.SetActive(true);
        }

        private void HideWindows()
        {
            if (stageWindow != null)
                stageWindow.SetActive(false);

            if (shopWindow != null)
                shopWindow.SetActive(false);

            if (boostSelectWindow != null)
                boostSelectWindow.Close();
        }

        private void InitializeExitBtn()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            // В WebGL Application.Quit() ничего не делает — мёртвая кнопка, модерация Яндекса такое режет.
            exitBtn.gameObject.SetActive(false);
#else
            exitBtn.onClick.AddListener(delegate
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
#endif
        }

        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;

            if (shopButton != null)
                shopButton.onClick.RemoveListener(ShowShop);

            if (stageButton != null)
                stageButton.onClick.RemoveListener(ShowStage);

            if (stageBackButton != null)
                stageBackButton.onClick.RemoveListener(ShowMain);
        }
    }
}
