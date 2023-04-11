using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(IncreaseEnemiesSettings),
        fileName = nameof(IncreaseEnemiesSettings))]
    public class IncreaseEnemiesSettings : ScriptableObject
    {
        [SerializeField] private int increaseHealthValue;
        [SerializeField] private float increaseSpeedValue;
        [SerializeField] private float timeDelayForBoost = 10;

        public int IncreaseHealthValue => increaseHealthValue;
        public float IncreaseSpeedValue => increaseSpeedValue;
        public float TimeDelayForBoost => timeDelayForBoost;
    }
}