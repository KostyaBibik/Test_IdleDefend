using UnityEngine;
using Views.Impl;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(EnemyPrefabsConfig),
        fileName = nameof(EnemyPrefabsConfig))]
    public class EnemyPrefabsConfig : ScriptableObject
    {
        [SerializeField] private EnemyView enemyView;
        [SerializeField] private float speedMoving;
        [SerializeField] private float spawnDelay;
        [SerializeField] private int rewardKillCoins = 50;
        [SerializeField] private ParticleSystem killEnemyParticles;
        
        public EnemyView EnemyView => enemyView;
        public float SpeedMoving => speedMoving;
        public float SpawnDelay => spawnDelay;
        public int RewardKillCoins => rewardKillCoins;
        public ParticleSystem KillEnemyParticles => killEnemyParticles;
    }
}