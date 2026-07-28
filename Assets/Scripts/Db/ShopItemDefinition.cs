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

        [Header("Механика (только Tab == Projectiles)")]
        [SerializeField] private ProjectileDefinition projectile;

        [Header("Бусты-расходники (только Tab == Boosts)")]
        [Tooltip("Тип эффекта, который будет применен на следующий бой после выбора буста на предбоевом экране.")]
        [SerializeField] private EBoostEffectType boostEffectType = EBoostEffectType.None;
        [Tooltip("Для процентов указывать долю: 0.10 = +10%. Для ShieldHits указывать количество блокируемых ударов.")]
        [SerializeField] private float boostValue;

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
        public ProjectileDefinition Projectile => projectile;
        public EBoostEffectType BoostEffectType => boostEffectType;
        public float BoostValue => boostValue;
    }
}
