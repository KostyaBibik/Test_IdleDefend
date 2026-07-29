using System.Collections;
using Agava.YandexGames;
using Game.Localization;
using GameAnalyticsSDK;
using Kimicu.YandexGames;
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

#if UNITY_EDITOR
        [SerializeField] private string locale = "ru";

#endif
        private bool _billingSuccses;

        private IEnumerator Start()
        {
            yield return YandexGamesSdk.Initialize();
            yield return Cloud.Initialize();
            Advertisement.Initialize();
            WebApplication.Initialize(OnStopGame);
            GameAnalytics.Initialize();
#if !UNITY_EDITOR
            yield return new WaitUntil(() => GameAnalytics.Initialized);
            yield return new WaitUntil(GameAnalytics.IsRemoteConfigsReady);
#endif
            yield return Billing.Initialize();
            yield return Consume();

            SaveSystem.Instance.Init();
            yield return LocalizationSettings.InitializationOperation;
            SetLanguage();
            Advertisement.ShowInterstitialAd();
            LoadScene();
        }

        private IEnumerator Consume()
        {
            Billing.GetPurchasedProducts(UpdateProductCatalog);
            yield return new WaitUntil(() => _billingSuccses);
        }

        private void UpdateProductCatalog(GetPurchasedProductsResponse response)
        {
            _billingSuccses = true;
            PurchasedProduct[] purchaseProducts = response.purchasedProducts;

            var countProducts = purchaseProducts.Length;
            for (var i = 0; i < countProducts; i++)
            {
                var product = purchaseProducts[i];
                if (product.productID.Equals(PurchaseIndexes.NoAD.ToString()))
                    SaveSystem.SaveData.NoAds = true;


                Billing.ConsumeProduct(product.purchaseToken);
            }
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


        private void LoadScene() => SceneManager.LoadScene(sceneBuildIndex: 1);


        internal enum PurchaseIndexes
        {
            NoAD
        }
    }
}
