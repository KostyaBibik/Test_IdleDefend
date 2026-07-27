using Enums;
using UnityEngine;

namespace Views.Impl
{
    public class SideTowerView : MonoBehaviour, IEntityView
    {
        [HideInInspector] public int attackDamage;
        [HideInInspector] public float attackSpeed;
        [HideInInspector] public float attackDistance;
        [HideInInspector] public float reloadRemaining;

        [HideInInspector] public ESideTowerAttackType attackType;

        [HideInInspector] public float beamPercentMaxHealthPerSecond;
        [HideInInspector] public float beamDamageAccumulator;
        [HideInInspector] public EnemyView beamCurrentTarget;
        [SerializeField] private LineRenderer beamLine;
        public LineRenderer BeamLine => beamLine;

        [HideInInspector] public int chainJumpCount;
        [HideInInspector] public float chainJumpRadius;
        [HideInInspector] public float chainFalloffFactor;
        [HideInInspector] public float chainVisualDuration;
        [HideInInspector] public float chainVisualRemaining;
        [SerializeField] private LineRenderer chainLine;
        public LineRenderer ChainLine => chainLine;
        [Tooltip("Материалы молнии для случайного выбора при каждом ударе. Можно оставить пустым — тогда используется материал, назначенный на LineRenderer.")]
        [SerializeField] private Material[] chainLineMaterials;
        public Material[] ChainLineMaterials => chainLineMaterials;

        [HideInInspector] public float slowPercent;

        public bool isDestroyed { get; set; }
    }
}
