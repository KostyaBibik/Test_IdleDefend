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
        
        [Header("Camera effects"), Space]
        [Header("On hurt tower shake effect settings:")]
        [SerializeField] private float shakeDuration;
        [SerializeField] private float shakeIntensity;
        
        public RewardView RewardEffect => rewardEffect;
        public float TimeShowReward => timeShowReward;
        public float SpeedAnimating => speedAnimating;
        public float ShakeDuration => shakeDuration;
        public float ShakeIntensity => shakeIntensity;
    }
}