using UI.Views.Game;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(VisualEffectsSettings),
        fileName = nameof(VisualEffectsSettings))]
    public class VisualEffectsSettings : ScriptableObject
    {
        [Header("Reward on screen effect")]
        [SerializeField] private RewardView rewardEffect;
        [SerializeField] private float timeShowReward;
        [SerializeField] private float speedAnimating;
        
        [Header("Floating damage numbers"), Space]
        [SerializeField] private RewardView damageNumberEffect;
        [SerializeField] private float damageNumberLifetime;
        [SerializeField] private float damageNumberRiseSpeed;
        [SerializeField, Tooltip("Разброс по горизонтали, чтобы совпавшие попадания не ложились стопкой")]
        private float damageNumberSpread;
        [SerializeField, Tooltip("Потолок одновременных чисел на экране - защита от каши при массовом уроне")]
        private int damageNumberMaxConcurrent;

        [Header("Damage number styles")]
        [SerializeField] private float damageNumberFontSize;
        [SerializeField] private Color damageNumberColor;
        [SerializeField] private float criticalFontSize;
        [SerializeField] private Color criticalColor;
        [SerializeField, Tooltip("Приписка к криту, например \"!\" или \" CRIT\"")]
        private string criticalSuffix;
        [SerializeField, Range(0.3f, 1f), Tooltip("Множитель размера для splash/pierce/chain")]
        private float secondaryHitScale;
        [SerializeField, Range(0.1f, 1f)] private float secondaryHitAlpha;

        [Header("Camera effects"), Space]
        [Header("On hurt tower shake effect settings:")]
        [SerializeField] private float shakeDuration;
        [SerializeField] private float shakeIntensity;

        public RewardView RewardEffect => rewardEffect;
        public float TimeShowReward => timeShowReward;
        public float SpeedAnimating => speedAnimating;
        public float ShakeDuration => shakeDuration;
        public float ShakeIntensity => shakeIntensity;

        public RewardView DamageNumberEffect => damageNumberEffect;
        public float DamageNumberLifetime => damageNumberLifetime;
        public float DamageNumberRiseSpeed => damageNumberRiseSpeed;
        public float DamageNumberSpread => damageNumberSpread;
        public int DamageNumberMaxConcurrent => damageNumberMaxConcurrent;
        public float DamageNumberFontSize => damageNumberFontSize;
        public Color DamageNumberColor => damageNumberColor;
        public float CriticalFontSize => criticalFontSize;
        public Color CriticalColor => criticalColor;
        public string CriticalSuffix => criticalSuffix;
        public float SecondaryHitScale => secondaryHitScale;
        public float SecondaryHitAlpha => secondaryHitAlpha;
    }
}