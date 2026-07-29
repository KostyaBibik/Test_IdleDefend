using System;
using System.Linq;
using Db;
using Enums;
using Kimicu.YandexGames;
using UnityEngine;

namespace Services
{
    public readonly struct ShopPurchaseResult
    {
        public ShopPurchaseResult(bool success, string reason)
        {
            Success = success;
            Reason = reason;
        }

        public bool Success { get; }
        public string Reason { get; }

        public static ShopPurchaseResult Ok()
        {
            return new ShopPurchaseResult(true, string.Empty);
        }

        public static ShopPurchaseResult Failed(string reason)
        {
            return new ShopPurchaseResult(false, reason);
        }
    }

    public static class YandexIapService
    {
        public static bool TryPurchase(ShopItemDefinition item, Action<ShopPurchaseResult> onComplete = null)
        {
            if (item == null || item.PurchaseType != EShopPurchaseType.Iap)
            {
                onComplete?.Invoke(ShopPurchaseResult.Failed("Item is not an IAP product."));
                return false;
            }

            if (string.IsNullOrWhiteSpace(item.IapProductId))
            {
                onComplete?.Invoke(ShopPurchaseResult.Failed("IAP product id is missing."));
                return false;
            }

#if UNITY_EDITOR
            Debug.Log($"[YandexIapService] Editor purchase simulation: {item.IapProductId}");
            ShopInventoryService.GrantIapItem(item);
            onComplete?.Invoke(ShopPurchaseResult.Ok());
            return true;
#else
            Billing.PurchaseProduct(item.IapProductId, response =>
            {
                ShopInventoryService.GrantIapItem(item);
                Billing.ConsumeProduct(response.purchaseData.purchaseToken);
                onComplete?.Invoke(ShopPurchaseResult.Ok());
            });

            return true;
#endif
        }

        public static bool TryGrantPendingProduct(string productId, ShopCatalogConfig catalog = null)
        {
            if (string.IsNullOrWhiteSpace(productId))
                return false;

            var item = catalog != null ? catalog.GetItemByIapProductId(productId) : null;
            if (item != null)
            {
                ShopInventoryService.GrantIapItem(item);
                return true;
            }

            return ShopInventoryService.TryGrantKnownIapProduct(productId);
        }

        public static string GetCatalogPrice(ShopItemDefinition item)
        {
            if (item == null || item.PurchaseType != EShopPurchaseType.Iap || string.IsNullOrWhiteSpace(item.IapProductId))
                return string.Empty;

            var products = Billing.CatalogProducts;
            if (products == null)
                return string.Empty;

            var product = products.FirstOrDefault(product => product.id == item.IapProductId);
            return product != null ? product.price.ToString() : string.Empty;
        }

        public static string GetCatalogCurrencyPictureUrl(ShopItemDefinition item)
        {
            if (item == null || item.PurchaseType != EShopPurchaseType.Iap || string.IsNullOrWhiteSpace(item.IapProductId))
                return string.Empty;

            var products = Billing.CatalogProducts;
            if (products == null)
                return string.Empty;

            var product = products.FirstOrDefault(product => product.id == item.IapProductId);
            return product != null ? product.priceCurrencyPicture : string.Empty;
        }
    }
}
