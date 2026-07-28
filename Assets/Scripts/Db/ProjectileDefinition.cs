using UnityEngine;
using Views.Impl;

namespace Db
{
    /// <summary>
    /// Механика конкретного снаряда: визуальный вариант BulletView + модификаторы урона/скорости
    /// полёта + спец-эффекты при попадании (заморозка/яд), независимые от типа атаки тела башни
    /// (EMainTowerAttackType). Привязывается к ShopItemDefinition с Tab == Projectiles; резолвится
    /// в бою через ShopInventoryService.GetEquippedItem, как TowerBodyDefinition для тела башни.
    /// </summary>
    [CreateAssetMenu(menuName = "Config/" + nameof(ProjectileDefinition), fileName = nameof(ProjectileDefinition))]
    public class ProjectileDefinition : ScriptableObject
    {
        [Header("Визуал")]
        [Tooltip("Prefab-вариант BulletView с собственным визуалом снаряда. Если не задан - используется " +
                 "базовый BulletConfigSettings.PrefabViewBullet.")]
        [SerializeField] private BulletView bulletPrefabVariant;

        [Header("Механика")]
        [Tooltip("Множитель урона выстрела относительно базового урона башни.")]
        [SerializeField, Min(0.1f)] private float damageMultiplier = 1f;
        [Tooltip("Множитель скорости полёта снаряда относительно базовой (BulletConfigSettings.SpeedMoving).")]
        [SerializeField, Min(0.1f)] private float speedMultiplier = 1f;

        [Header("Спец-эффект - заморозка (не зависит от типа атаки башни)")]
        [Tooltip("Применяет тот же фрост-эффект (EnemyView.ApplyFrost), что и Frost-тело башни, но как " +
                 "отдельный источник - работает вместе с любым типом атаки.")]
        [SerializeField] private bool appliesFrost;
        [SerializeField, Range(0f, 1f)] private float frostSlowPercent = 0.4f;
        [SerializeField, Min(0f)] private float frostSlowDuration = 2f;

        [Header("Спец-эффект - яд (периодический урон, не зависит от типа атаки башни)")]
        [SerializeField] private bool appliesPoison;
        [Tooltip("Урон за один тик = % от урона попадания, которое наложило яд (не фиксированное число) - " +
                 "так эффект масштабируется вместе с уроном башни/бустами, а не отстаёт от них.")]
        [SerializeField, Range(0f, 2f)] private float poisonDamagePercentPerTick = 0.3f;
        [SerializeField, Min(0.05f)] private float poisonTickInterval = 0.5f;
        [SerializeField, Min(0f)] private float poisonDuration = 3f;
        [Tooltip("Партикл яда на враге, пока действует эффект (зелёный, по аналогии с обледенением)")]
        [SerializeField] private GameObject poisonVfxPrefab;

        public BulletView BulletPrefabVariant => bulletPrefabVariant;
        public float DamageMultiplier => damageMultiplier;
        public float SpeedMultiplier => speedMultiplier;

        public bool AppliesFrost => appliesFrost;
        public float FrostSlowPercent => frostSlowPercent;
        public float FrostSlowDuration => frostSlowDuration;

        public bool AppliesPoison => appliesPoison;
        public float PoisonDamagePercentPerTick => poisonDamagePercentPerTick;
        public float PoisonTickInterval => poisonTickInterval;
        public float PoisonDuration => poisonDuration;
        public GameObject PoisonVfxPrefab => poisonVfxPrefab;
    }
}
