using System;
using System.Linq;
using Db;
using Enums;

namespace Services
{
    public static class ShopInventoryService
    {
        public static event Action OnChanged;

        public static bool IsOwned(ShopItemDefinition item)
        {
            if (item == null)
                return false;

            if (BoostInventoryService.IsBoostItem(item))
                return BoostInventoryService.GetCount(item) > 0;

            if (item.OwnedByDefault)
                return true;

            EnsureSaveData();
            return SaveSystem.SaveData.PurchasedShopItemIds.Contains(item.Id);
        }

        public static bool IsEquipped(ShopItemDefinition item)
        {
            if (item == null || !item.Equippable)
                return false;

            EnsureSaveData();
            return SaveSystem.SaveData.EquippedShopItemIds.TryGetValue(item.Tab.ToString(), out var equippedId)
                   && equippedId == item.Id;
        }

        public static EShopItemState GetState(ShopItemDefinition item)
        {
            if (item == null)
                return EShopItemState.Available;

            if (BoostInventoryService.IsBoostItem(item))
            {
                return item.PurchaseType == EShopPurchaseType.Emeralds && EmeraldWallet.Balance < item.EmeraldPrice
                    ? EShopItemState.NotEnoughCurrency
                    : EShopItemState.Available;
            }

            if (IsEquipped(item))
                return EShopItemState.Equipped;

            if (IsOwned(item))
                return EShopItemState.Owned;

            return item.PurchaseType == EShopPurchaseType.Emeralds && EmeraldWallet.Balance < item.EmeraldPrice
                ? EShopItemState.NotEnoughCurrency
                : EShopItemState.Available;
        }

        public static ShopPurchaseResult TryPurchase(ShopItemDefinition item)
        {
            if (item == null)
                return ShopPurchaseResult.Failed("Item is missing.");

            if (BoostInventoryService.IsBoostItem(item))
                return BoostInventoryService.TryPurchase(item, 1);

            if (IsOwned(item))
                return ShopPurchaseResult.Ok();

            switch (item.PurchaseType)
            {
                case EShopPurchaseType.Free:
                    AddOwned(item);
                    return ShopPurchaseResult.Ok();

                case EShopPurchaseType.Emeralds:
                    if (!EmeraldWallet.TrySpend(item.EmeraldPrice))
                        return ShopPurchaseResult.Failed("Not enough emeralds.");

                    AddOwned(item);
                    return ShopPurchaseResult.Ok();

                case EShopPurchaseType.Iap:
                    return YandexIapService.TryPurchase(item)
                        ? ShopPurchaseResult.Ok()
                        : ShopPurchaseResult.Failed("IAP purchase failed to start.");

                default:
                    return ShopPurchaseResult.Failed("Unsupported purchase type.");
            }
        }

        public static void GrantIapItem(ShopItemDefinition item)
        {
            if (item == null || item.PurchaseType != EShopPurchaseType.Iap)
                return;

            if (item.Tab == EShopTab.GemPack && item.GemRewardAmount > 0)
                EmeraldWallet.Add(item.GemRewardAmount);
            else
                AddOwned(item);

            SaveSystem.Instance.SaveToStorage();
            RaiseChanged();
        }

        public static bool TryGrantKnownIapProduct(string iapProductId)
        {
            var reward = GetKnownGemPackReward(iapProductId);
            if (reward <= 0)
                return false;

            EmeraldWallet.Add(reward);
            SaveSystem.Instance.SaveToStorage();
            RaiseChanged();
            return true;
        }

        public static bool TryEquip(ShopItemDefinition item)
        {
            if (BoostInventoryService.IsBoostItem(item))
                return false;

            if (item == null || !item.Equippable || !IsOwned(item))
                return false;

            EnsureSaveData();
            SaveSystem.SaveData.EquippedShopItemIds[item.Tab.ToString()] = item.Id;
            SaveSystem.Instance.SaveToStorage();
            RaiseChanged();
            return true;
        }

        public static string GetEquippedItemId(EShopTab tab)
        {
            EnsureSaveData();
            return SaveSystem.SaveData.EquippedShopItemIds.TryGetValue(tab.ToString(), out var itemId)
                ? itemId
                : string.Empty;
        }

        public static ShopItemDefinition GetEquippedItem(ShopCatalogConfig catalog, EShopTab tab)
        {
            if (catalog == null)
                return null;

            EnsureDefaultEquipped(catalog, tab);
            return catalog.GetItem(GetEquippedItemId(tab));
        }

        public static void EnsureDefaultEquipped(ShopCatalogConfig catalog, EShopTab tab)
        {
            if (catalog == null || !string.IsNullOrEmpty(GetEquippedItemId(tab)))
                return;

            var defaultItem = catalog.GetItems(tab).FirstOrDefault(item => item.OwnedByDefault && item.Equippable);

            if (defaultItem != null)
                TryEquip(defaultItem);
        }

        private static void AddOwned(ShopItemDefinition item)
        {
            EnsureSaveData();

            if (!SaveSystem.SaveData.PurchasedShopItemIds.Contains(item.Id))
                SaveSystem.SaveData.PurchasedShopItemIds.Add(item.Id);

            SaveSystem.Instance.SaveToStorage();
            RaiseChanged();
        }

        private static void EnsureSaveData()
        {
            SaveSystem.EnsureRuntimeCollections();
        }

        private static void RaiseChanged()
        {
            PlayerSaveData.NotifyShopInventoryChanged();
            OnChanged?.Invoke();
        }

        private static int GetKnownGemPackReward(string iapProductId)
        {
            return iapProductId switch
            {
                "gems_small" => 100,
                "gems_medium" => 550,
                "gems_large" => 1200,
                "gems_huge" => 2500,
                _ => 0
            };
        }
    }
}
