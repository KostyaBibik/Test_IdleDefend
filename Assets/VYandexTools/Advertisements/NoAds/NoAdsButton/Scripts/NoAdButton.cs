using System.Collections;
using System.Linq;
using DefaultNamespace.Yandex;
using Kimicu.YandexGames;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using VYandexTools.Advertisements.NoAds.NoAdsButton.Scripts;

public class NoAdButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private RawImage priceImage;
    [SerializeField] private TextAdapter priceText;
    
    private void Awake()
    {
        if (SaveSystem.SaveData.NoAds == false)
            button.onClick.AddListener(BuyNoAd);
        else OnBuyNoAds();

        priceText.Text = Billing.CatalogProducts.FirstOrDefault(x => x.id == Boot.PurchaseIndexes.NoAD.ToString())
            ?.price.ToString();
        var picture = Billing.CatalogProducts[0].priceCurrencyPicture;
        StartCoroutine(DownloadImage(picture));
        PlayerSaveData.OnBuyNoAds += OnBuyNoAds;
    }

    private void OnBuyNoAds() => button.gameObject.SetActive(false);

    private void BuyNoAd()
    {
        var productId = Boot.PurchaseIndexes.NoAD.ToString();

        Billing.PurchaseProduct(productId, (purchaseProductResponse) =>
        {
            Billing.ConsumeProduct(purchaseProductResponse.purchaseData.purchaseToken);
            SaveSystem.SaveData.NoAds = true;

            // NoAds покупается мимо магазина игры, поэтому событие шлём отсюда — иначе
            // эта покупка не попадёт в аналитику вообще.
            GaEventProvider.PurchaseById(productId, itemType: "NoAds");
        });
    }

    private IEnumerator DownloadImage(string textureUrl)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(textureUrl);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
            Debug.Log("Download Image Error");
        else
        {
            var texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            priceImage.texture = texture;
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        button ??= GetComponent<Button>();
    }
#endif
}