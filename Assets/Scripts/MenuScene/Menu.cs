using Db;
using Enums;
using Services;
using UI;
using UI.Views.Shop;
using UnityEngine;
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
        [Header("Windows")]
        [SerializeField] private GameObject mainWindow;
        [SerializeField] private GameObject stageWindow;
        [SerializeField] private GameObject shopWindow;
        [SerializeField] private BoostSelectWindowView boostSelectWindow;
        [Header("Navigation")]
        [SerializeField] private Button shopButton;
        [SerializeField] private Button stageButton;
        [SerializeField] private Button stageBackButton;

        private void Start()
        {
            InitializeNavigationButtons();
            levelPathBuilder.Build(levelsConfig, OpenBoostSelect);
            InitializeExitBtn();
            HideWindows();
        }

        /// <summary>
        /// Клик по уровню на карте открывает выбор бустов на бой вместо немедленной загрузки
        /// GameScene. Если экран не назначен, ведём себя как раньше и грузим уровень сразу.
        /// </summary>
        private void OpenBoostSelect(int levelIndex)
        {
            if (boostSelectWindow == null)
            {
                PlayLevel(levelIndex);
                return;
            }

            boostSelectWindow.Open(levelIndex, PlayLevel);
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
            if (shopButton != null)
                shopButton.onClick.RemoveListener(ShowShop);

            if (stageButton != null)
                stageButton.onClick.RemoveListener(ShowStage);

            if (stageBackButton != null)
                stageBackButton.onClick.RemoveListener(ShowMain);
        }
    }
}
