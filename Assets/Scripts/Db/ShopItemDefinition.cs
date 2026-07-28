using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/Shop/" + nameof(ShopItemDefinition),
        fileName = nameof(ShopItemDefinition))]
    public class ShopItemDefinition : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private EShopTab tab;
        [SerializeField] private EShopPurchaseType purchaseType = EShopPurchaseType.Emeralds;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private int emeraldPrice;
        [SerializeField] private string iapProductId;
        [SerializeField] private int gemRewardAmount;
        [SerializeField] private bool ownedByDefault;
        [SerializeField] private bool equippable = true;
        [SerializeField] private int sortOrder;

        [Header("Механика (только Tab == Tower)")]
        [SerializeField] private TowerBodyDefinition towerBody;

        public string Id => id;
        public EShopTab Tab => tab;
        public EShopPurchaseType PurchaseType => purchaseType;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject PreviewPrefab => previewPrefab;
        public int EmeraldPrice => emeraldPrice;
        public string IapProductId => iapProductId;
        public int GemRewardAmount => gemRewardAmount;
        public bool OwnedByDefault => ownedByDefault;
        public bool Equippable => equippable;
        public int SortOrder => sortOrder;
        public TowerBodyDefinition TowerBody => towerBody;
    }
}
