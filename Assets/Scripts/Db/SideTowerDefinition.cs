using UnityEngine;
using Views.Impl;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(SideTowerDefinition), fileName = nameof(SideTowerDefinition))]
    public class SideTowerDefinition : ScriptableObject
    {
        [Header("Отображение")]
        [SerializeField] private string displayName;
        [SerializeField] private Sprite icon;
        [SerializeField] private SideTowerView viewPrefab;

        [Header("Характеристики")]
        [SerializeField] private int cost = 100;
        [SerializeField] private int attackDamage = 20;
        [SerializeField] private float attackSpeed = 1f;
        [SerializeField] private float attackDistance = 3f;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public SideTowerView ViewPrefab => viewPrefab;

        public int Cost => cost;
        public int AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float AttackDistance => attackDistance;
    }
}
