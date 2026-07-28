using Db;
using Enums;

namespace UI.Views.Shop
{
    /// <summary>
    /// Общее представление товара для всех мест, где он показывается (карточка сетки, попап
    /// превью, будущие витрины) - чтобы цена не форматировалась в трёх местах по-разному.
    /// </summary>
    public static class ShopItemPresenter
    {
        public static string GetPriceText(ShopItemDefinition item)
        {
            if (item == null)
                return string.Empty;

            return item.PurchaseType switch
            {
                EShopPurchaseType.Free => "Бесплатно",
                EShopPurchaseType.Emeralds => item.EmeraldPrice.ToString(),
                EShopPurchaseType.Iap => item.GemRewardAmount > 0 ? item.GemRewardAmount.ToString("N0") : "IAP",
                _ => string.Empty
            };
        }
    }
}
