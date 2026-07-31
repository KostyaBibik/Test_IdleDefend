using System;
using Db;
using Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

namespace Game.Localization
{
    public static class GameLocalization
    {
        private const string TableName = "LocalizationTable";
        public const string EmptyLocaleCode = "em";

        public static bool IsEmptyLocale =>
            LocalizationSettings.HasSettings &&
            LocalizationSettings.SelectedLocale != null &&
            string.Equals(
                LocalizationSettings.SelectedLocale.Identifier.Code,
                EmptyLocaleCode,
                StringComparison.OrdinalIgnoreCase);

        public static string Text(LocalizationKey key, string fallback = "")
        {
            return Text(key.ToString(), fallback);
        }

        public static string Text(string key, string fallback = "")
        {
            if (string.IsNullOrEmpty(key))
                return fallback;

            try
            {
                if (!LocalizationSettings.HasSettings || LocalizationSettings.SelectedLocale == null)
                    return fallback;

                if (IsEmptyLocale)
                    return string.Empty;

                return LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);
            }
            catch
            {
                return fallback;
            }
        }

        public static string Format(LocalizationKey key, string fallback, params object[] values)
        {
            var format = Text(key, fallback);

            if (string.IsNullOrEmpty(format))
                return format;

            try
            {
                return string.Format(format, values);
            }
            catch (FormatException)
            {
                return fallback;
            }
        }

        public static string ShopItemName(ShopItemDefinition item)
        {
            return item == null ? string.Empty : Text($"shop_item_{item.Id}_name", item.DisplayName);
        }

        public static string ShopItemDescription(ShopItemDefinition item)
        {
            return item == null ? string.Empty : Text($"shop_item_{item.Id}_description", item.Description);
        }

        public static string TowerBuffName(TowerBuffDefinition buff)
        {
            return buff == null ? string.Empty : Text($"tower_buff_{buff.Id}_name", buff.DisplayName);
        }

        public static string TowerBuffDescription(TowerBuffDefinition buff)
        {
            return buff == null ? string.Empty : Text($"tower_buff_{buff.Id}_description", buff.Description);
        }

        public static string SideTowerName(SideTowerDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            string key = definition.name switch
            {
                "SideTower_Beam" => LocalizationKey.side_tower_beam_name.ToString(),
                "SideTower_ChainLightning" => LocalizationKey.side_tower_chain_lightning_name.ToString(),
                "SideTower_Cheap" => LocalizationKey.side_tower_projectiles_name.ToString(),
                "SideTower_SlowAura" => LocalizationKey.side_tower_slow_aura_name.ToString(),
                _ => null
            };

            return key == null ? definition.DisplayName : Text(key, definition.DisplayName);
        }

        public static string ShopTab(EShopTab tab)
        {
            return tab switch
            {
                EShopTab.Tower => Text(LocalizationKey.shop_tab_tower, tab.ToString()),
                EShopTab.Projectiles => Text(LocalizationKey.shop_tab_projectiles, tab.ToString()),
                EShopTab.Boosts => Text(LocalizationKey.shop_tab_boosts, tab.ToString()),
                EShopTab.GemPack => Text(LocalizationKey.shop_tab_gem_pack, tab.ToString()),
                _ => tab.ToString()
            };
        }

        public static string ShopState(EShopItemState state)
        {
            return state switch
            {
                EShopItemState.Available => Text(LocalizationKey.shop_state_available, state.ToString()),
                EShopItemState.NotEnoughCurrency => Text(LocalizationKey.shop_state_not_enough_currency, state.ToString()),
                EShopItemState.Owned => Text(LocalizationKey.shop_state_owned, state.ToString()),
                EShopItemState.Equipped => Text(LocalizationKey.shop_state_equipped, state.ToString()),
                _ => state.ToString()
            };
        }

        public static void SetButtonLabel(Button button, LocalizationKey key, string fallback)
        {
            if (button == null)
                return;

            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = Text(key, fallback);
        }

        public static TMP_Text FindTextByCurrentValue(Component root, params string[] values)
        {
            if (root == null || values == null || values.Length == 0)
                return null;

            var labels = root.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                if (label == null)
                    continue;

                foreach (var value in values)
                {
                    if (string.Equals(label.text, value, StringComparison.OrdinalIgnoreCase))
                        return label;
                }
            }

            return null;
        }

        public static void SetTextByCurrentValue(Component root, LocalizationKey key, string fallback, params string[] values)
        {
            var label = FindTextByCurrentValue(root, values);
            if (label != null)
                label.text = Text(key, fallback);
        }
    }
}
