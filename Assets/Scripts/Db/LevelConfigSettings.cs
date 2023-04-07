using System.Collections.Generic;
using UnityEngine;
using Views.Impl;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(LevelConfigSettings),
        fileName = nameof(LevelConfigSettings))]
    public class LevelConfigSettings : ScriptableObject
    {
        [SerializeField] private int levelCounter;
        [Space]
        [SerializeField] private List<EnemyView> enemies;
        [SerializeField] private float spawnDelay;
        [SerializeField] private int countEnemies;

        public int LevelCounter => levelCounter;
        public List<EnemyView> Enemies => enemies;
        public float SpawnDelay => spawnDelay;
        public int CountEnemies => countEnemies;
    }
}