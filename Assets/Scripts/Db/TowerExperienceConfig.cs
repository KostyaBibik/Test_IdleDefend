using System;
using System.Collections.Generic;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(TowerExperienceConfig),
        fileName = nameof(TowerExperienceConfig))]
    public class TowerExperienceConfig : ScriptableObject
    {
        [Header("Опыт за врагов")]
        [SerializeField, Min(1)] private int defaultExperiencePerEnemy = 5;
        [SerializeField, Min(0f)] private float enemyExperienceMultiplier = 1f;

        [Header("Опыт до уровня")]
        [Tooltip("targetLevel = уровень, который получит башня. experience = сколько опыта нужно набрать с предыдущего уровня.")]
        [SerializeField] private List<LevelExperienceRequirement> levelRequirements = new();

        public int MaxConfiguredLevel
        {
            get
            {
                var max = 1;
                foreach (var requirement in levelRequirements)
                    max = Mathf.Max(max, requirement.TargetLevel);

                return max;
            }
        }

        public bool HasNextLevel(int currentLevel)
        {
            return GetRequirement(currentLevel + 1) != null;
        }

        public int GetExperienceToNextLevel(int currentLevel)
        {
            var requirement = GetRequirement(currentLevel + 1);
            return requirement != null ? requirement.Experience : 0;
        }

        public int GetEnemyExperience(EnemyDefinition enemyDefinition)
        {
            var baseReward = enemyDefinition != null && enemyDefinition.ExperienceReward > 0
                ? enemyDefinition.ExperienceReward
                : defaultExperiencePerEnemy;

            return Mathf.Max(1, Mathf.CeilToInt(baseReward * enemyExperienceMultiplier));
        }

        private LevelExperienceRequirement GetRequirement(int targetLevel)
        {
            for (var i = 0; i < levelRequirements.Count; i++)
            {
                var requirement = levelRequirements[i];
                if (requirement.TargetLevel == targetLevel)
                    return requirement;
            }

            return null;
        }
    }

    [Serializable]
    public class LevelExperienceRequirement
    {
        [SerializeField, Min(2)] private int targetLevel;
        [SerializeField, Min(1)] private int experience;

        public int TargetLevel => targetLevel;
        public int Experience => experience;
    }
}
