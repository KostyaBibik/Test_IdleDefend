using System;
using System.Collections.Generic;
using Enums;
using UnityEngine;
using Views.Impl;
using Random = UnityEngine.Random;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(EnemyPrefabsConfig),
        fileName = nameof(EnemyPrefabsConfig))]
    public class EnemyPrefabsConfig : ScriptableObject
    {
        [SerializeField] private float minSpawnDelay = 2;
        [SerializeField] private float maxSpawnDelay = 4;
        [Space] [SerializeField] private EnemyPrefab[] prefabs;

        public float MinSpawnDelay => minSpawnDelay;
        public float MaxSpawnDelay => maxSpawnDelay;
        public int CountPrefabs => prefabs.Length;

        public EnemyPrefab GetPrefab(EEnemyType type)
        {
            foreach (var prefab in prefabs)
            {
                if (prefab.type == type)
                    return prefab;
            }

            throw new Exception($"[EnemyPrefabsConfig] Can't find prefab with type: {type}");
        }

        [Serializable]
        public class EnemyPrefab
        {
            public EEnemyType type;
            public EnemyView view;
            public float speedMoving;
            public int startHealth;
            public int rewardKillCoins;
            public List<ParticleSystem> killEnemyParticles;

            public ParticleSystem GetRandomParticle()
            {
                return killEnemyParticles[Random.Range(0, killEnemyParticles.Count)];
            }
        }
    }
}