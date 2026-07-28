using System;
using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;

namespace Services
{
    public static class BoostInventoryService
    {
        public static event Action OnChanged;

        public static bool IsBoostItem(ShopItemDefinition item)
        {
            return item != null && item.Tab == EShopTab.Boosts && item.BoostEffectType != EBoostEffectType.None;
        }

        public static int GetCount(ShopItemDefinition item)
        {
            return item == null ? 0 : GetCount(item.Id);
        }

        public static int GetCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            EnsureSaveData();
            return SaveSystem.SaveData.BoostItemCounts.TryGetValue(itemId, out var count) ? Math.Max(0, count) : 0;
        }

        public static bool IsSelected(ShopItemDefinition item)
        {
            if (!IsBoostItem(item))
                return false;

            EnsureSaveData();
            return SaveSystem.SaveData.SelectedBoostItemIds.Contains(item.Id);
        }

        public static IReadOnlyList<string> GetSelectedBoostIds()
        {
            EnsureSaveData();
            return SaveSystem.SaveData.SelectedBoostItemIds.ToArray();
        }

        public static ShopPurchaseResult TryPurchase(ShopItemDefinition item, int quantity)
        {
            if (!IsBoostItem(item))
                return ShopPurchaseResult.Failed("Boost item is missing.");

            if (quantity <= 0)
                return ShopPurchaseResult.Failed("Quantity must be positive.");

            if (item.PurchaseType != EShopPurchaseType.Emeralds && item.PurchaseType != EShopPurchaseType.Free)
                return ShopPurchaseResult.Failed("Only free and emerald boost purchases are supported.");

            var totalPrice = item.EmeraldPrice * quantity;
            if (item.PurchaseType == EShopPurchaseType.Emeralds && totalPrice > 0 && !EmeraldWallet.TrySpend(totalPrice))
                return ShopPurchaseResult.Failed("Not enough emeralds.");

            Add(item.Id, quantity);
            return ShopPurchaseResult.Ok();
        }

        public static bool SetSelected(ShopItemDefinition item, bool selected)
        {
            if (!IsBoostItem(item))
                return false;

            EnsureSaveData();
            var selectedIds = SaveSystem.SaveData.SelectedBoostItemIds;

            if (!selected)
            {
                var removed = selectedIds.Remove(item.Id);
                if (removed)
                    SaveAndRaiseChanged();

                return removed;
            }

            if (GetCount(item) <= 0)
                return false;

            if (selectedIds.Contains(item.Id))
                return true;

            selectedIds.Add(item.Id);
            SaveAndRaiseChanged();
            return true;
        }

        public static bool ToggleSelected(ShopItemDefinition item)
        {
            return SetSelected(item, !IsSelected(item));
        }

        public static IReadOnlyList<ShopItemDefinition> ConsumeSelectedBoosts(ShopCatalogConfig catalog)
        {
            EnsureSaveData();

            if (catalog == null || SaveSystem.SaveData.SelectedBoostItemIds.Count == 0)
                return Array.Empty<ShopItemDefinition>();

            var consumed = new List<ShopItemDefinition>();
            var selectedIds = SaveSystem.SaveData.SelectedBoostItemIds.ToArray();

            foreach (var boostId in selectedIds)
            {
                var item = catalog.GetItem(boostId);
                if (!IsBoostItem(item) || GetCount(boostId) <= 0)
                    continue;

                Add(boostId, -1, false);
                consumed.Add(item);
            }

            // The pre-battle choice is a one-run commitment. Clear it after battle start so
            // returning to the arena later cannot silently spend another set of consumables.
            SaveSystem.SaveData.SelectedBoostItemIds.Clear();

            SaveAndRaiseChanged();
            return consumed;
        }

        private static void Add(string itemId, int delta, bool save = true)
        {
            EnsureSaveData();

            var current = GetCount(itemId);
            var next = Math.Max(0, current + delta);

            if (next == 0)
                SaveSystem.SaveData.BoostItemCounts.Remove(itemId);
            else
                SaveSystem.SaveData.BoostItemCounts[itemId] = next;

            if (save)
                SaveAndRaiseChanged();
        }

        private static void EnsureSaveData()
        {
            SaveSystem.EnsureRuntimeCollections();
        }

        private static void SaveAndRaiseChanged()
        {
            SaveSystem.Instance.SaveToStorage();
            PlayerSaveData.NotifyShopInventoryChanged();
            OnChanged?.Invoke();
        }
    }
}
