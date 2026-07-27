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
        [SerializeField] private List<WaveDefinition> waves;

        [Header("Доп-башни: индексы слотов сцены, доступных для покупки на этом уровне")]
        [SerializeField] private List<int> unlockedSideTowerSlotIndices;

        [Space]
        [Header("Звёзды: сколько секунд нужно продержаться")]
        [Tooltip("Продержался хотя бы столько секунд — 1 звезда")]
        [SerializeField] private float star1Seconds = 30f;
        [Tooltip("Продержался хотя бы столько секунд — 2 звезды")]
        [SerializeField] private float star2Seconds = 60f;
        [Tooltip("Продержался хотя бы столько секунд — 3 звезды. После этого момента новые враги перестают спавниться")]
        [SerializeField] private float star3Seconds = 90f;

        public int LevelId => levelId;
        public List<WaveDefinition> Waves => waves;
        public List<int> UnlockedSideTowerSlotIndices => unlockedSideTowerSlotIndices;
        public float Star1Seconds => star1Seconds;
        public float Star2Seconds => star2Seconds;
        public float Star3Seconds => star3Seconds;

        public int CalculateStars(float elapsedSeconds)
        {
            if (elapsedSeconds >= star3Seconds)
                return 3;
            if (elapsedSeconds >= star2Seconds)
                return 2;
            return 1;
        }

        public float GetTriggerSeconds(EWaveStartTrigger trigger)
        {
            switch (trigger)
            {
                case EWaveStartTrigger.AfterStar1Threshold:
                    return star1Seconds;
                case EWaveStartTrigger.AfterStar2Threshold:
                    return star2Seconds;
                default:
                    return 0f;
            }
        }
    }

    [Serializable]
    public class WaveDefinition
    {
        public EEnemyType enemyType;
        public int count = 5;
        public float spawnDelay = 1f;
        public int extraHealth;
        public float extraSpeed;
        public EWaveStartTrigger startTrigger = EWaveStartTrigger.LevelStart;
    }
}
