using Enums;
using UnityEngine;

namespace Views.Impl
{
    public class TowerView : MonoBehaviour, IEntityView
    {
        [SerializeField] private Transform sphere;

        [HideInInspector] public float ratioRange = 0.5f;
        [HideInInspector] public float attackDistance;
        [HideInInspector] public float attackSpeed;
        [HideInInspector] public int attackDamage;

        [Header("Тело башни (заполняется TowerInitializeSystem из экипированного товара магазина)")]
        [HideInInspector] public EMainTowerAttackType attackType = EMainTowerAttackType.Default;

        [HideInInspector] public int pierceCount;
        [HideInInspector] public float pierceJumpRadius;
        [HideInInspector] public float pierceFalloff;
        [HideInInspector] public float pierceLineDuration;
        [HideInInspector] public float pierceLineRemaining;
        [Tooltip("Линия-разряд, показывающая, кого пробил снаряд. Присутствует только у prefab-варианта с Pierce (Crystal) — у остальных null, это нормально.")]
        [SerializeField] private LineRenderer pierceLine;
        public LineRenderer PierceLine => pierceLine;

        [HideInInspector] public float frostSlowPercent;
        [HideInInspector] public float frostSlowDuration;

        [HideInInspector] public float splashRadius;
        [HideInInspector] public float splashFalloff;
        [HideInInspector] public GameObject splashImpactEffectPrefab;
        [HideInInspector] public float splashImpactEffectReferenceRadius = 1f;

        [Header("Ультимативная способность")]
        [HideInInspector] public string ultimateName;
        [HideInInspector] public Sprite ultimateIcon;
        [HideInInspector] public float ultimateCooldown;
        [HideInInspector] public float ultimateCooldownRemaining;

        [HideInInspector] public float barrageAttackSpeedMultiplier;
        [HideInInspector] public float barrageDuration;
        [HideInInspector] public float shatterDamagePercentOfMaxHealth;
        [HideInInspector] public float freezeDuration;
        [HideInInspector] public float freezeWaveSpeed;
        [HideInInspector] public GameObject freezeWaveBurstEffectPrefab;
        [HideInInspector] public float overloadSplashRadiusMultiplier;
        [HideInInspector] public float overloadDuration;

        public Transform Sphere => sphere;
        public bool isDestroyed { get; set; }
    }
}
