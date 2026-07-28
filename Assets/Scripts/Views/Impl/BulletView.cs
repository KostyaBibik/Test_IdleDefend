using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Views.Impl
{
    public class BulletView : MonoBehaviour, IEntityView
    {
        [HideInInspector] public EnemyView target;
        [HideInInspector] public int damage;

        [Tooltip("Тип атаки башни, выпустившей снаряд - определяет, какой доп. эффект BulletHitSystem применит по факту попадания (не в момент выстрела).")]
        [HideInInspector] public EMainTowerAttackType attackType = EMainTowerAttackType.Default;

        [HideInInspector] public float splashRadius;
        [HideInInspector] public float splashFalloff;
        [HideInInspector] public GameObject splashImpactEffectPrefab;
        [HideInInspector] public float splashImpactEffectReferenceRadius = 1f;

        [HideInInspector] public float frostSlowPercent;
        [HideInInspector] public float frostSlowDuration;

        [HideInInspector] public int pierceCount;
        [HideInInspector] public float pierceJumpRadius;
        [HideInInspector] public float pierceFalloff;

        [Tooltip("Множитель скорости полёта относительно BulletConfigSettings.SpeedMoving - задаётся выбранным в магазине снарядом.")]
        [HideInInspector] public float speedMultiplier = 1f;

        [Tooltip("Спец-эффекты выбранного в магазине снаряда - применяются дополнительно к attackType тела башни (не заменяют его).")]
        [HideInInspector] public bool projectileAppliesFrost;
        [HideInInspector] public float projectileFrostSlowPercent;
        [HideInInspector] public float projectileFrostSlowDuration;
        [HideInInspector] public bool projectileAppliesPoison;
        [HideInInspector] public float projectilePoisonDamagePercentPerTick;
        [HideInInspector] public float projectilePoisonTickInterval;
        [HideInInspector] public float projectilePoisonDuration;
        [HideInInspector] public GameObject projectilePoisonVfxPrefab;

        [HideInInspector] public Vector3 launchPosition;
        [HideInInspector] public Vector3 launchDirection;
        [HideInInspector] public int ricochetRemaining;
        [HideInInspector] public float ricochetRadius;
        [HideInInspector] public float ricochetFalloff;
        [HideInInspector] public int piercingLineRemaining;
        [HideInInspector] public float piercingLineWidth;
        [HideInInspector] public float piercingLineFalloff;
        [HideInInspector] public float piercingLineRange;
        [HideInInspector] public List<EnemyView> hitEnemies = new();

        public bool isDestroyed { get; set; }
    }
}
