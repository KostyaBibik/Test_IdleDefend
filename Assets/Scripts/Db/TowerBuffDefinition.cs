using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/Tower Buffs/" + nameof(TowerBuffDefinition),
        fileName = nameof(TowerBuffDefinition))]
    public class TowerBuffDefinition : ScriptableObject
    {
        [Header("Основное")]
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private ETowerBuffRarity rarity = ETowerBuffRarity.Common;
        [SerializeField] private ETowerBuffEffectType effectType = ETowerBuffEffectType.None;

        [Header("Баланс")]
        [Tooltip("Для процентов указывать долю: 0.15 = +15%. Для HealTower/MaxHealth указывать количество HP.")]
        [SerializeField] private float value;
        [SerializeField, Min(1)] private int maxStacks = 1;
        [SerializeField, Min(0f)] private float weight = 1f;
        [Tooltip("Если выключено, баф остается в данных, но не выпадает в level-up выборе.")]
        [SerializeField] private bool enabledInRoll = true;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public ETowerBuffRarity Rarity => rarity;
        public ETowerBuffEffectType EffectType => effectType;
        public float Value => value;
        public int MaxStacks => maxStacks;
        public float Weight => weight;
        public bool EnabledInRoll => enabledInRoll;
    }
}
