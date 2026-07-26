using System.Collections;
using Agava.YandexGames;
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
            string lang = locale;
#else
             string lang = YandexGamesSdk.Environment.i18n.lang;
#endif
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(lang);
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