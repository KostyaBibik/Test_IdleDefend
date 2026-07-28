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

        public bool isDestroyed { get; set; }
    }
}