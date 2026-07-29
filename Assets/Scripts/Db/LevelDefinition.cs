using System;
using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(LevelDefinition),
        fileName = nameof(LevelDefinition))]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] private int levelId;
        [Tooltip("Стартовые монеты внутри боевой сессии. Позволяет балансировать темп апгрейдов отдельно для каждого уровня.")]
        [SerializeField, Min(0)] private int startCoins = 120;
        [Header("Секции спавна: 1 = старт-star1, 2 = star1-star2, 3 = star2-star3")]
        [SerializeField] private List<SpawnSectionDefinition> spawnSections = new();

        [Tooltip("Эмеральды, начисляемые за полную зачистку уровня (см. WinActionSystem)")]
        [SerializeField] private int rewardEmeralds = 50;

        [Header("Доп-башни: индексы слотов сцены, доступных для покупки на этом уровне")]
        [SerializeField] private List<int> unlockedSideTowerSlotIndices;

        [Space]
        [Header("Тайминги волн и прогресс-бара (3 звезды даются за полную зачистку уровня, см. WinActionSystem)")]
        [Tooltip("Момент запуска волн с триггером AfterStar1Threshold; также чекпоинт на прогресс-баре")]
        [SerializeField] private float star1Seconds = 30f;
        [Tooltip("Момент запуска волн с триггером AfterStar2Threshold; также чекпоинт на прогресс-баре")]
        [SerializeField] private float star2Seconds = 60f;
        [Tooltip("После этого момента новые враги перестают спавниться (IsSpawnCapped); также чекпоинт на прогресс-баре")]
        [SerializeField] private float star3Seconds = 90f;

        [Space]
        [Header("Награда за врагов")]
        [Tooltip("Множитель монет до первого порога звезды.")]
        [SerializeField] private float rewardMultiplierAtStart = 1f;
        [Tooltip("Множитель монет после первого порога звезды.")]
        [SerializeField] private float rewardMultiplierAtStar1 = 1.1f;
        [Tooltip("Множитель монет после второго порога звезды.")]
        [SerializeField] private float rewardMultiplierAtStar2 = 1.2f;
        [Tooltip("Множитель монет после третьего порога звезды.")]
        [SerializeField] private float rewardMultiplierAtStar3 = 1.3f;
        [Tooltip("Потолок плавного роста награды со временем. 0.6 = максимум +60% к фазовому множителю.")]
        [SerializeField] private float rewardTimeGrowthLimit = 0.6f;
        [Tooltip("Чем больше значение, тем медленнее награда приближается к потолку роста.")]
        [SerializeField] private float rewardTimeGrowthSeconds = 120f;
        [Tooltip("Округление награды до красивого шага. 5 = 73 округлится до 75.")]
        [SerializeField] private int rewardRoundTo = 5;

        public int LevelId => levelId;
        public int StartCoins => startCoins;
        public IReadOnlyList<SpawnSectionDefinition> SpawnSections => spawnSections;
        public int RewardEmeralds => rewardEmeralds;
        public List<int> UnlockedSideTowerSlotIndices => unlockedSideTowerSlotIndices;
        public float Star1Seconds => star1Seconds;
        public float Star2Seconds => star2Seconds;
        public float Star3Seconds => star3Seconds;
        public float RewardMultiplierAtStart => rewardMultiplierAtStart;
        public float RewardMultiplierAtStar1 => rewardMultiplierAtStar1;
        public float RewardMultiplierAtStar2 => rewardMultiplierAtStar2;
        public float RewardMultiplierAtStar3 => rewardMultiplierAtStar3;
        public float RewardTimeGrowthLimit => rewardTimeGrowthLimit;
        public float RewardTimeGrowthSeconds => rewardTimeGrowthSeconds;
        public int RewardRoundTo => rewardRoundTo;

        public int CalculateEnemyReward(int baseReward, float elapsedSeconds)
        {
            var reward = baseReward * GetRewardMultiplier(elapsedSeconds);
            return RoundReward(Mathf.Max(1, Mathf.RoundToInt(reward)));
        }

        private float GetRewardMultiplier(float elapsedSeconds)
        {
            var phaseMultiplier = GetRewardPhaseMultiplier(elapsedSeconds);
            var growthSeconds = Mathf.Max(0.01f, rewardTimeGrowthSeconds);
            var timeMultiplier = 1f + Mathf.Max(0f, rewardTimeGrowthLimit) *
                (1f - Mathf.Exp(-Mathf.Max(0f, elapsedSeconds) / growthSeconds));

            return phaseMultiplier * timeMultiplier;
        }

        private float GetRewardPhaseMultiplier(float elapsedSeconds)
        {
            if (elapsedSeconds >= star3Seconds)
                return rewardMultiplierAtStar3;
            if (elapsedSeconds >= star2Seconds)
                return rewardMultiplierAtStar2;
            if (elapsedSeconds >= star1Seconds)
                return rewardMultiplierAtStar1;

            return rewardMultiplierAtStart;
        }

        private int RoundReward(int reward)
        {
            if (rewardRoundTo <= 1)
                return reward;

            return Mathf.Max(rewardRoundTo, Mathf.RoundToInt((float) reward / rewardRoundTo) * rewardRoundTo);
        }

        public SpawnSectionDefinition GetSpawnSection(float elapsedSeconds)
        {
            if (spawnSections == null || spawnSections.Count == 0)
                return null;

            var index = GetSpawnSectionIndex(elapsedSeconds);
            return index >= 0 && index < spawnSections.Count ? spawnSections[index] : null;
        }

        public int GetSpawnSectionIndex(float elapsedSeconds)
        {
            if (elapsedSeconds >= star3Seconds)
                return -1;

            if (elapsedSeconds >= star2Seconds)
                return 2;

            if (elapsedSeconds >= star1Seconds)
                return 1;

            return 0;
        }
    }

    [Serializable]
    public class SpawnSectionDefinition
    {
        [SerializeField] private List<EnemySpawnEntryDefinition> enemies = new();

        public IReadOnlyList<EnemySpawnEntryDefinition> Enemies => enemies;
    }

    [Serializable]
    public class EnemySpawnEntryDefinition
    {
        [Header("Враг")]
        public EEnemyType enemyType;

        [Header("Задержка спавна")]
        [Min(0.05f)] public float spawnDelayMin = 1f;
        [Min(0.05f)] public float spawnDelayMax = 1f;

        [Header("Модификаторы")]
        public int extraHealth;
        public float extraSpeed;
    }
}
