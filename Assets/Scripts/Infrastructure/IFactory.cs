using Enums;
using UnityEngine;
using Views.Impl;

namespace Infrastructure
{
    public interface IFactory
    {
        EnemyView CreateEnemy(Vector3 posSpawn, EEnemyType type, int additiveHealth, float additiveSpeed,
            int experienceRewardOverride = -1, bool grantCoinReward = true, bool grantExperienceReward = true,
            float healthMultiplier = 1f);
    }
}
