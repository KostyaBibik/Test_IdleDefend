using Components;
using Db;
using Enums;
using UnityEngine;
using UnityEngine.UI;

namespace Views.Impl
{
    public class EnemyView : MonoBehaviour, IEntityView
    {
        [SerializeField] private Transform mesh;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("Партикл 'обледенения' (Epic Toon FX Snow), выключен по умолчанию — включается на время frostTimeRemaining")]
        [SerializeField] private GameObject frostVfx;

        private Color _baseColor;

        [HideInInspector] public HealthComponent healthComponent;
        [HideInInspector] public float speedMoving;
        [HideInInspector] public EEnemyType type;
        [HideInInspector] public EnemyDefinition definition;
        [HideInInspector] public int experienceRewardOverride = -1;
        [HideInInspector] public bool grantCoinReward = true;
        [HideInInspector] public bool grantExperienceReward = true;
        [HideInInspector] public float tutorialDamageTakenMultiplier = 1f;

        [HideInInspector] public float orbitAngleDeg = float.NaN;
        [HideInInspector] public float orbitRadius = -1f;

        [HideInInspector] public float speedMultiplier = 1f;

        [Tooltip("Тайм-замедление от Frost-башни/ультимейта 'Заморозка'. Резолвится в EnemySpeedModifierSystem.")]
        [HideInInspector] public float frostSpeedMultiplier = 1f;
        [HideInInspector] public float frostTimeRemaining;

        [Tooltip("Периодический урон (яд) от Poison-снаряда. Резолвится в EnemyPoisonSystem / PreviewCombatDriver.")]
        [HideInInspector] public float poisonTimeRemaining;
        [HideInInspector] public float poisonTickRemaining;
        [HideInInspector] public float poisonDamagePerTick;
        [HideInInspector] public float poisonTickInterval;
        [HideInInspector] public GameObject poisonVfxPrefab;
        private GameObject _poisonVfxInstance;

        /// <summary>
        /// Единая точка применения фрост-эффекта (пассив Frost-башни и ультимейт "Заморозка" оба
        /// идут через неё): более слабый эффект не имеет права прервать/ослабить уже активный
        /// более сильный (например, догнавший в полёте пассивный снаряд не должен снимать заморозку
        /// от ультимейта раньше времени) — только обновить длительность, если сила равна или выше.
        /// </summary>
        public void ApplyFrost(float speedMultiplier, float duration)
        {
            if (frostTimeRemaining > 0f && frostSpeedMultiplier < speedMultiplier)
                return;

            frostSpeedMultiplier = speedMultiplier;
            frostTimeRemaining = duration;
        }

        /// <summary>
        /// Единая точка применения яда (по аналогии с ApplyFrost): освежает длительность/тик и
        /// запоминает партикл конкретного снаряда - у разных ядовитых снарядов он может отличаться.
        /// </summary>
        public void ApplyPoison(float damagePerTick, float tickInterval, float duration, GameObject vfxPrefab)
        {
            poisonDamagePerTick = damagePerTick;
            poisonTickInterval = Mathf.Max(0.05f, tickInterval);
            poisonTimeRemaining = duration;
            poisonTickRemaining = poisonTickInterval;
            poisonVfxPrefab = vfxPrefab;
        }

        /// <summary>Партикл яда создаётся лениво на активации и уничтожается на деактивации - в отличие
        /// от frostVfx он не встроен в префаб врага, потому что разные снаряды несут разный партикл.</summary>
        public GameObject PoisonVfxInstance => _poisonVfxInstance;

        public void SetPoisonVisual(bool active)
        {
            if (active)
            {
                if (_poisonVfxInstance == null && poisonVfxPrefab != null)
                    _poisonVfxInstance = Instantiate(poisonVfxPrefab, transform.position, Quaternion.identity, transform);

                return;
            }

            if (_poisonVfxInstance == null)
                return;

            Destroy(_poisonVfxInstance);
            _poisonVfxInstance = null;
        }

        public Transform Mesh => mesh;
        public Slider HealthSlider => healthSlider;
        public bool isDestroyed { get; set; }
        public int poolVersion { get; set; }

        private void Awake()
        {
            if (spriteRenderer != null)
                _baseColor = spriteRenderer.color;

            if (frostVfx != null)
                frostVfx.SetActive(false);
        }

        public void PlayDeathAnimation()
        {
            if (animator != null)
                animator.SetTrigger("Death");
            else
                mesh.gameObject.SetActive(false);
        }

        /// <summary>
        /// Вызывается EntityPoolService.Return перед тем, как объект уйдёт в пул - сбрасывает
        /// всё временное боевое состояние, чтобы следующий Rent (уже под другого врага) не
        /// унаследовал фрост/яд/анимацию смерти от предыдущей жизни этого GameObject-а.
        /// </summary>
        public void ResetForPool()
        {
            SetPoisonVisual(false);
            poisonTimeRemaining = 0f;
            poisonTickRemaining = 0f;
            poisonDamagePerTick = 0f;
            poisonTickInterval = 0f;
            poisonVfxPrefab = null;

            SetFrostVisual(false, 1f);
            frostSpeedMultiplier = 1f;
            frostTimeRemaining = 0f;

            orbitAngleDeg = float.NaN;
            orbitRadius = -1f;
            speedMultiplier = 1f;

            experienceRewardOverride = -1;
            grantCoinReward = true;
            grantExperienceReward = true;
            tutorialDamageTakenMultiplier = 1f;

            if (animator != null)
                animator.Rebind();
            if (mesh != null)
                mesh.gameObject.SetActive(true);
        }

        /// <summary>
        /// Синхронизирует визуал "обледенения" (тинт + партикл + скорость анимации)
        /// с фактическим фрост-эффектом. Дёргается из EnemySpeedModifierSystem каждый тик,
        /// а не только по фронту, т.к. frostSpeedMultiplier может смениться (пассив -> ультимейт).
        /// </summary>
        public void SetFrostVisual(bool active, float animatorSpeedMultiplier)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = active ? Color.Lerp(_baseColor, FrostTintColor, FrostTintStrength) : _baseColor;

            if (frostVfx != null && frostVfx.activeSelf != active)
                frostVfx.SetActive(active);

            if (animator != null)
                animator.speed = active ? animatorSpeedMultiplier : 1f;
        }

        private static readonly Color FrostTintColor = new Color(0.55f, 0.85f, 1f);
        private const float FrostTintStrength = 0.6f;
    }
}
