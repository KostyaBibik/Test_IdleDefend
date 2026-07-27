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
        [SerializeField] private EffectConnector beamEffectConnector;
        public EffectConnector BeamEffectConnector => beamEffectConnector;

        [HideInInspector] public int chainJumpCount;
        [HideInInspector] public float chainJumpRadius;
        [HideInInspector] public float chainFalloffFactor;
        [HideInInspector] public float chainVisualRemaining;
        [SerializeField] private LineRenderer chainLine;
        public LineRenderer ChainLine => chainLine;

        [HideInInspector] public float slowPercent;

        public bool isDestroyed { get; set; }
    }
}
