using Enums;
using UnityEngine;
using Views.Impl;

namespace Db
{
    /// <summary>
    /// Механика конкретного тела главной башни: тип пассивной атаки + параметры,
    /// плюс её фирменная ультимативная способность. Привязывается к ShopItemDefinition
    /// с Tab == Tower; резолвится в бою через ShopInventoryService.GetEquippedItem.
    /// </summary>
    [CreateAssetMenu(menuName = "Config/" + nameof(TowerBodyDefinition), fileName = nameof(TowerBodyDefinition))]
    public class TowerBodyDefinition : ScriptableObject
    {
        [Header("Тип атаки")]
        [SerializeField] private EMainTowerAttackType attackType = EMainTowerAttackType.Default;

        [Header("Визуал")]
        [Tooltip("Prefab-вариант TowerView с уже настроенным под это тело визуалом (цвет сферы, аура и т.п.). " +
                 "GameInstaller инстанцирует именно его вместо базового TowerConfigSettings.PrefabViewTower.")]
        [SerializeField] private TowerView towerPrefabVariant;

        [Header("Pierce — попадание прыгает по ближайшим врагам (как ChainLightning у доп-башен)")]
        [SerializeField, Min(1)] private int pierceCount = 3;
        [SerializeField, Min(0f)] private float pierceJumpRadius = 2f;
        [SerializeField, Range(0f, 1f)] private float pierceFalloff = 0.75f;
        [Tooltip("Сколько секунд видна линия-разряд через пробитых врагов")]
        [SerializeField, Min(0f)] private float pierceLineDuration = 0.15f;

        [Header("Frost — попадание замедляет врага на время")]
        [SerializeField, Range(0f, 1f)] private float frostSlowPercent = 0.4f;
        [SerializeField, Min(0f)] private float frostSlowDuration = 2f;

        [Header("Splash — попадание задевает площадь вокруг цели")]
        [SerializeField, Min(0f)] private float splashRadius = 1.5f;
        [SerializeField, Range(0f, 1f)] private float splashFalloff = 0.5f;
        [Tooltip("Разовый эффект в точке попадания, наглядно показывающий радиус сплэша")]
        [SerializeField] private GameObject splashImpactEffectPrefab;
        [Tooltip("При каком splashRadius эффект показан в масштабе x1 — дальше масштабируется пропорционально (в т.ч. во время 'Перегрузки')")]
        [SerializeField, Min(0.01f)] private float splashImpactEffectReferenceRadius = 1.5f;

        [Header("Ультимативная способность")]
        [SerializeField] private string ultimateName;
        [SerializeField] private Sprite ultimateIcon;
        [SerializeField, Min(0.1f)] private float ultimateCooldown = 20f;

        [Space]
        [Tooltip("Барраж (Default): множитель скорости атаки")]
        [SerializeField, Min(1f)] private float barrageAttackSpeedMultiplier = 3f;
        [SerializeField, Min(0f)] private float barrageDuration = 5f;

        [Tooltip("Раскол (Pierce): % от maxHP каждому врагу на экране")]
        [SerializeField, Range(0f, 1f)] private float shatterDamagePercentOfMaxHealth = 0.5f;

        [Tooltip("Заморозка (Frost): полностью останавливает всех врагов на экране")]
        [SerializeField, Min(0f)] private float freezeDuration = 3f;
        [Tooltip("Скорость расширения кольца заморозки (юниты/сек) - враг замерзает в момент, когда фронт волны его касается, а не мгновенно все разом")]
        [SerializeField, Min(0.1f)] private float freezeWaveSpeed = 20f;
        [Tooltip("Разовый партикл-вспышка в момент активации ультимейта (например Epic Toon FX NovaFrost)")]
        [SerializeField] private GameObject freezeWaveBurstEffectPrefab;

        [Tooltip("Перегрузка (Splash): множитель радиуса сплэша")]
        [SerializeField, Min(1f)] private float overloadSplashRadiusMultiplier = 2f;
        [SerializeField, Min(0f)] private float overloadDuration = 5f;

        public EMainTowerAttackType AttackType => attackType;

        public TowerView TowerPrefabVariant => towerPrefabVariant;

        public int PierceCount => pierceCount;
        public float PierceJumpRadius => pierceJumpRadius;
        public float PierceFalloff => pierceFalloff;
        public float PierceLineDuration => pierceLineDuration;

        public float FrostSlowPercent => frostSlowPercent;
        public float FrostSlowDuration => frostSlowDuration;

        public float SplashRadius => splashRadius;
        public float SplashFalloff => splashFalloff;
        public GameObject SplashImpactEffectPrefab => splashImpactEffectPrefab;
        public float SplashImpactEffectReferenceRadius => splashImpactEffectReferenceRadius;

        public string UltimateName => ultimateName;
        public Sprite UltimateIcon => ultimateIcon;
        public float UltimateCooldown => ultimateCooldown;

        public float BarrageAttackSpeedMultiplier => barrageAttackSpeedMultiplier;
        public float BarrageDuration => barrageDuration;

        public float ShatterDamagePercentOfMaxHealth => shatterDamagePercentOfMaxHealth;

        public float FreezeDuration => freezeDuration;
        public float FreezeWaveSpeed => freezeWaveSpeed;
        public GameObject FreezeWaveBurstEffectPrefab => freezeWaveBurstEffectPrefab;

        public float OverloadSplashRadiusMultiplier => overloadSplashRadiusMultiplier;
        public float OverloadDuration => overloadDuration;
    }
}
