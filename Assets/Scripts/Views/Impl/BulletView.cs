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

        /// <summary>
        /// Сколько урона этот снаряд зарезервировал на цели через
        /// EnemyHealthComponent.ReduceAssumedHealth и ещё не отдал. Резерв нужен, чтобы башни не
        /// расстреливали врага, который уже гарантированно убит летящими в него снарядами
        /// (см. EnemyService.GetAssumedActiveEnemies). Но если снаряд исчез, не попав, резерв
        /// обязан вернуться: иначе живой враг навсегда считается мёртвым и по нему никто не
        /// стреляет — он спокойно доходит до башни.
        /// </summary>
        [HideInInspector] public int reservedDamage;

        /// <summary>
        /// Полный сброс рантайм-состояния. Снаряды берутся из пула (EntityPoolService), поэтому
        /// без сброса новый выстрел наследует поля предыдущего: тип атаки, сплэш, рикошет,
        /// направление свободного полёта, список уже задетых врагов. Вызывается из фабрики —
        /// стрелку остаётся заполнить только то, что относится к его выстрелу.
        /// </summary>
        public void ResetRuntimeState()
        {
            target = null;
            targetPoolVersion = 0;
            damage = 0;
            reservedDamage = 0;
            isCritical = false;
            attackType = EMainTowerAttackType.Default;

            splashRadius = 0f;
            splashFalloff = 0f;
            splashImpactEffectPrefab = null;
            splashImpactEffectReferenceRadius = 1f;

            frostSlowPercent = 0f;
            frostSlowDuration = 0f;

            pierceCount = 0;
            pierceJumpRadius = 0f;
            pierceFalloff = 0f;

            speedMultiplier = 1f;

            projectileAppliesFrost = false;
            projectileFrostSlowPercent = 0f;
            projectileFrostSlowDuration = 0f;
            projectileAppliesPoison = false;
            projectilePoisonDamagePercentPerTick = 0f;
            projectilePoisonTickInterval = 0f;
            projectilePoisonDuration = 0f;
            projectilePoisonVfxPrefab = null;

            launchPosition = Vector3.zero;
            launchDirection = Vector3.zero;
            previousPosition = transform.position;
            freeFlightDirection = Vector3.zero;
            freeFlightRemainingDistance = 0f;
            freeFlightRemainingSeconds = 0f;
            freeFlightCollisionRadius = 0.18f;
            continueOnTargetLost = false;

            ricochetRemaining = 0;
            ricochetRadius = 0f;
            ricochetFalloff = 0f;
            piercingLineRemaining = 0;
            piercingLineWidth = 0f;
            piercingLineFalloff = 0f;
            piercingLineRange = 0f;
            explosiveShotRadius = 0f;
            explosiveShotFalloff = 0f;

            hitEnemies.Clear();
        }

        /// <summary>Зарезервировать урон на цели, запомнив это на снаряде (см. reservedDamage).</summary>
        public void ReserveDamageOnTarget()
        {
            if (target == null)
                return;

            reservedDamage += damage;
            target.healthComponent.ReduceAssumedHealth(damage);
        }

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
