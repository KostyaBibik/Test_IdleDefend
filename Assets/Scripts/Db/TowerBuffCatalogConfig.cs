using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/Tower Buffs/" + nameof(TowerBuffCatalogConfig),
        fileName = nameof(TowerBuffCatalogConfig))]
    public class TowerBuffCatalogConfig : ScriptableObject
    {
        [SerializeField] private List<TowerBuffDefinition> buffs = new();

        [Header("Шансы редкости")]
        [SerializeField, Min(0f)] private float commonChance = 70f;
        [SerializeField, Min(0f)] private float rareChance = 25f;
        [SerializeField, Min(0f)] private float legendaryChance = 5f;

        [Header("Выбор")]
        [SerializeField, Min(1)] private int choicesCount = 3;

        public IReadOnlyList<TowerBuffDefinition> Buffs => buffs;
        public float CommonChance => commonChance;
        public float RareChance => rareChance;
        public float LegendaryChance => legendaryChance;
        public int ChoicesCount => choicesCount;

        public IEnumerable<TowerBuffDefinition> GetEnabledBuffs()
        {
            return buffs.Where(buff => buff != null && buff.EnabledInRoll);
        }

        public IEnumerable<TowerBuffDefinition> GetEnabledBuffs(ETowerBuffRarity rarity)
        {
            return GetEnabledBuffs().Where(buff => buff.Rarity == rarity);
        }
    }
}
