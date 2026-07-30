using System.Collections;
using Agava.YandexGames;
using Db;
using Game.Localization;
using GameAnalyticsSDK;
using Kimicu.YandexGames;
using Services;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using Billing = Kimicu.YandexGames.Billing;
using WebApplication = Kimicu.YandexGames.WebApplication;
using YandexGamesSdk = Kimicu.YandexGames.YandexGamesSdk;

namespace DefaultNamespace.Yandex
{
    public class Boot : MonoBehaviour
    {
        private const string DefaultLocaleCode = "en";
        private const float GameAnalyticsInitializationTimeoutSeconds = 5f;
        private const float CloudInitializationTimeoutSeconds = 5f;
        private const float BillingInitializationTimeoutSeconds = 8f;
        private const float PurchasedProductsTimeoutSeconds = 5f;
        [SerializeField] private ShopCatalogConfig shopCatalogConfig;

#if UNITY_EDITOR
        [SerializeField] private string locale = "ru";

#endif
        private bool _purchasedProductsRequestFinished;

        private IEnumerator Start()
        {
            yield return YandexGamesSdk.Initialize();
            yield return InitializeCloud();
            Advertisement.Initialize();
            WebApplication.Initialize(OnStopGame);
            yield return InitializeGameAnalytics();
            yield return InitializeBilling();

            if (Billing.Initialized)
                yield return Consume();

            SaveSystem.Instance.Init();
            yield return LocalizationSettings.InitializationOperation;
            SetLanguage();
            LoadScene();
        }

        private IEnumerator InitializeCloud()
        {
            Coroutine initialization = StartCoroutine(Cloud.Initialize());
            float deadline = Time.realtimeSinceStartup + CloudInitializationTimeoutSeconds;

            while (!Cloud.Initialized && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (Cloud.Initialized)
                yield break;

            StopCoroutine(initialization);
            Debug.LogWarning("Cloud save is unavailable. Continuing with local storage.");
        }

        private IEnumerator InitializeBilling()
        {
            Coroutine initialization = StartCoroutine(Billing.Initialize());
            float deadline = Time.realtimeSinceStartup + BillingInitializationTimeoutSeconds;

            while (!Billing.Initialized && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (Billing.Initialized)
                yield break;

            StopCoroutine(initialization);
            Debug.LogWarning("Billing is unavailable. Continuing with purchases disabled.");
        }

        private static IEnumerator InitializeGameAnalytics()
        {
            GameAnalytics.Initialize();

#if !UNITY_EDITOR
            float deadline = Time.realtimeSinceStartup + GameAnalyticsInitializationTimeoutSeconds;
            while (!GameAnalytics.IsRemoteConfigsReady() && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!GameAnalytics.IsRemoteConfigsReady())
                Debug.LogWarning("GameAnalytics remote config is unavailable. Continuing with local defaults.");
#endif
            yield break;
        }

        private IEnumerator Consume()
        {
            _purchasedProductsRequestFinished = false;
            Billing.GetPurchasedProducts(UpdateProductCatalog, OnPurchasedProductsError);

            float deadline = Time.realtimeSinceStartup + PurchasedProductsTimeoutSeconds;
            while (!_purchasedProductsRequestFinished && Time.realtimeSinceStartup < deadline)
                yield return null;

            if (!_purchasedProductsRequestFinished)
                Debug.LogWarning("Pending purchases request timed out. The game will continue without blocking startup.");
        }

        private void UpdateProductCatalog(GetPurchasedProductsResponse response)
        {
            _purchasedProductsRequestFinished = true;
            PurchasedProduct[] purchaseProducts = response?.purchasedProducts;

            if (purchaseProducts == null)
                return;

            var countProducts = purchaseProducts.Length;
            for (var i = 0; i < countProducts; i++)
            {
                var product = purchaseProducts[i];
                if (product.productID.Equals(PurchaseIndexes.NoAD.ToString()))
                {
                    SaveSystem.SaveData.NoAds = true;
                    Billing.ConsumeProduct(product.purchaseToken);
                    continue;
                }

                if (YandexIapService.TryGrantPendingProduct(product.productID, shopCatalogConfig))
                    Billing.ConsumeProduct(product.purchaseToken);
            }
        }

        private void OnPurchasedProductsError(string error)
        {
            _purchasedProductsRequestFinished = true;
            Debug.LogWarning($"Pending purchases are unavailable: {error}");
        }

        private void SetLanguage()
        {
#if UNITY_EDITOR
            string lang = LocalizationTestOverride.HasLocaleCode ? LocalizationTestOverride.LocaleCode : locale;
#else
            string lang = YandexGamesSdk.Environment.i18n.lang;
#endif
            LocalizationSettings.SelectedLocale = GetAvailableLocale(lang);
        }

        private static Locale GetAvailableLocale(string lang)
        {
            var locales = LocalizationSettings.AvailableLocales;
            var selectedLocale = locales.GetLocale(lang);

            if (selectedLocale != null)
                return selectedLocale;

            selectedLocale = locales.GetLocale(DefaultLocaleCode);

            if (selectedLocale != null)
                return selectedLocale;

            return locales.Locales.Count > 0 ? locales.Locales[0] : null;
        }

        private static void OnStopGame(bool value)
        {
            AudioListener.volume = value ? 1 : 0;
            AudioListener.pause = !value;
            Time.timeScale = value ? 1 : 0;
        }


        private void LoadScene()
        {
            SaveSystem.EnsureRuntimeCollections();
            ref var saveData = ref SaveSystem.SaveData;
            var isFirstGame = !saveData.HasStartedFirstGame
                              && saveData.TutorialVersion <= 0
                              && saveData.UnlockedLevelIndex <= 0
                              && saveData.LevelStars.Count == 0;

            if (!isFirstGame)
            {
                SceneManager.LoadScene(sceneBuildIndex: 1);
                return;
            }

            saveData.HasStartedFirstGame = true;
            SaveSystem.Instance.SaveToStorage();
            SelectedLevelHolder.SelectedLevelIndex = 0;
            SceneManager.LoadScene("GameScene");
        }


        internal enum PurchaseIndexes
        {
            NoAD
        }
    }
}
