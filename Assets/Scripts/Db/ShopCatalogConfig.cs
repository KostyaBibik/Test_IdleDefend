using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/Shop/" + nameof(ShopCatalogConfig),
        fileName = nameof(ShopCatalogConfig))]
    public class ShopCatalogConfig : ScriptableObject
    {
        [SerializeField] private List<ShopItemDefinition> items = new();

        public IReadOnlyList<ShopItemDefinition> Items => items;

        public IEnumerable<ShopItemDefinition> GetItems(EShopTab tab)
        {
            return items
                .Where(item => item != null && item.Tab == tab)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.DisplayName);
        }

        public ShopItemDefinition GetItem(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            return items.FirstOrDefault(item => item != null && item.Id == id);
        }
    }
}
