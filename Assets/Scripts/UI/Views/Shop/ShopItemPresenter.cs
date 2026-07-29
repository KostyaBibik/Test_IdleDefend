using Db;
using Enums;
using Game.Localization;

namespace UI.Views.Shop
{
    public static class ShopItemPresenter
    {
        public static string GetPriceText(ShopItemDefinition item)
        {
            if (item == null)
                return string.Empty;

            return item.PurchaseType switch
            {
                EShopPurchaseType.Free => GameLocalization.Text(LocalizationKey.shop_free, "Free"),
                EShopPurchaseType.Emeralds => item.EmeraldPrice.ToString(),
                EShopPurchaseType.Iap => item.GemRewardAmount > 0
                    ? item.GemRewardAmount.ToString("N0")
                    : GameLocalization.Text(LocalizationKey.shop_iap, "IAP"),
                _ => string.Empty
            };
        }
    }
}
