using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(VisualEffectsConfigSettings),
        fileName = nameof(VisualEffectsConfigSettings))]
    public class VisualEffectsConfigSettings : ScriptableObject
    {
        [Header("Reward on screen effect")]
        [SerializeField] private GameObject rewardEffect;
        [SerializeField] private float timeShowReward;
        [SerializeField] private float speedAnimating;
        
        public GameObject RewardEffect => rewardEffect;
        public float TimeShowReward => timeShowReward;
        public float SpeedAnimating => speedAnimating;
    }
}