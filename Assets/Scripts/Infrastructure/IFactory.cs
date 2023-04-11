using Enums;
using UnityEngine;

namespace Infrastructure
{
    public interface IFactory
    {
        void CreateEnemy(Vector3 posSpawn, EEnemyType type, int additiveHealth, float additiveSpeed);
    }
}