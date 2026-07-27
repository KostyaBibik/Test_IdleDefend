using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(LevelsConfig),
        fileName = nameof(LevelsConfig))]
    public class LevelsConfig : ScriptableObject
    {
        [SerializeField] private List<LevelDefinition> levels;

        public int Count => levels.Count;
        public IReadOnlyList<LevelDefinition> Levels => levels;

        public LevelDefinition GetByIndex(int index)
        {
            if (index < 0 || index >= levels.Count)
                throw new Exception($"[LevelsConfig] Index out of range: {index}");

            return levels[index];
        }

        public int IndexOf(LevelDefinition level)
        {
            return levels.IndexOf(level);
        }

        public LevelDefinition GetById(int levelId)
        {
            var level = levels.FirstOrDefault(l => l.LevelId == levelId);
            if (level == null)
                throw new Exception($"[LevelsConfig] Can't find level with id: {levelId}");

            return level;
        }
    }
}
