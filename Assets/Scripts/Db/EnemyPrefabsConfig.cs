using System;
using System.Collections.Generic;
using System.Linq;
using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(EnemyPrefabsConfig),
        fileName = nameof(EnemyPrefabsConfig))]
    public class EnemyPrefabsConfig : ScriptableObject
    {
        [SerializeField] private float minSpawnDelay = 2;
        [SerializeField] private float maxSpawnDelay = 4;
        [Space] [SerializeField] private List<EnemyDefinition> definitions;

        public float MinSpawnDelay => minSpawnDelay;
        public float MaxSpawnDelay => maxSpawnDelay;
        public int CountPrefabs => definitions.Count;

        public EnemyDefinition GetPrefab(EEnemyType type)
        {
            var definition = definitions.FirstOrDefault(d => d.Type == type);
            if (definition == null)
                throw new Exception($"[EnemyPrefabsConfig] Can't find prefab with type: {type}");

            return definition;
        }
    }
}
