using Enums;
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

        [Header("Тип атаки")]
        [SerializeField] private ESideTowerAttackType attackType = ESideTowerAttackType.Projectile;

        [Header("Beam")]
        [Tooltip("Процент от MAX HP цели в секунду (0.1 = 10%/сек)")]
        [SerializeField, Range(0f, 1f)] private float beamPercentMaxHealthPerSecond = 0.1f;

        [Header("Chain Lightning")]
        [SerializeField] private int chainJumpCount = 2;
        [SerializeField] private float chainJumpRadius = 2f;
        [Tooltip("Множитель урона за каждый следующий прыжок (0.7 = 70% от предыдущего)")]
        [SerializeField, Range(0f, 1f)] private float chainFalloffFactor = 0.7f;
        [Tooltip("Сколько секунд виден визуал молнии после удара")]
        [SerializeField] private float chainVisualDuration = 0.15f;

        [Header("Slow Aura")]
        [Tooltip("Замедление скорости врагов в радиусе (0.4 = -40%)")]
        [SerializeField, Range(0f, 1f)] private float slowPercent = 0.4f;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public SideTowerView ViewPrefab => viewPrefab;

        public int Cost => cost;
        public int AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float AttackDistance => attackDistance;

        public ESideTowerAttackType AttackType => attackType;

        public float BeamPercentMaxHealthPerSecond => beamPercentMaxHealthPerSecond;

        public int ChainJumpCount => chainJumpCount;
        public float ChainJumpRadius => chainJumpRadius;
        public float ChainFalloffFactor => chainFalloffFactor;
        public float ChainVisualDuration => chainVisualDuration;

        public float SlowPercent => slowPercent;
    }
}
