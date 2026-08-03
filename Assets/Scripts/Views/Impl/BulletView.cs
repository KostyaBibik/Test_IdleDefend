using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Views.Impl
{
    public class BulletView : MonoBehaviour, IEntityView
    {
        [HideInInspector] public EnemyView target;
        [Tooltip("poolVersion цели на момент назначения target - см. IEntityView.poolVersion. " +
                 "Без этого снаряд, летящий дольше кадра, может решить, что переиспользованный " +
                 "под нового врага GameObject - всё ещё его исходная цель.")]
        [HideInInspector] public int targetPoolVersion;
        [HideInInspector] public int damage;
        [HideInInspector] public bool isCritical;

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
        [HideInInspector] public Vector3 previousPosition;
        [HideInInspector] public Vector3 freeFlightDirection;
        [HideInInspector] public float freeFlightRemainingDistance;
        [HideInInspector] public float freeFlightRemainingSeconds;
        [HideInInspector] public float freeFlightCollisionRadius = 0.18f;
        [HideInInspector] public bool continueOnTargetLost;
        [HideInInspector] public int ricochetRemaining;
        [HideInInspector] public float ricochetRadius;
        [HideInInspector] public float ricochetFalloff;
        [HideInInspector] public int piercingLineRemaining;
        [HideInInspector] public float piercingLineWidth;
        [HideInInspector] public float piercingLineFalloff;
        [HideInInspector] public float piercingLineRange;
        [HideInInspector] public float explosiveShotRadius;
        [HideInInspector] public float explosiveShotFalloff;
        [Tooltip("Пары (враг, poolVersion на момент попадания) - версия нужна по той же причине, " +
                 "что и targetPoolVersion: без неё переиспользованный под нового врага объект " +
                 "ошибочно считался бы 'уже подбитым этим снарядом'.")]
        [HideInInspector] public List<HitRecord> hitEnemies = new();

        public bool isDestroyed { get; set; }
        public int poolVersion { get; set; }

        public readonly struct HitRecord
        {
            public readonly EnemyView View;
            public readonly int Version;

            public HitRecord(EnemyView view, int version)
            {
                View = view;
                Version = version;
            }
        }
    }
}
