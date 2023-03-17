using UnityEngine;

namespace Infrastructure
{
    public interface IFactory
    {
        void CreateEnemy(Vector3 posSpawn);
    }
}