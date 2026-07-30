using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(TutorialScenarioConfig),
        fileName = nameof(TutorialScenarioConfig))]
    public sealed class TutorialScenarioConfig : ScriptableObject
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(1)] private int version = 1;
        [SerializeField] private bool forceInEditor;
        [SerializeField, Min(0)] private int levelIndex;

        [Header("Intro combat")]
        [SerializeField, Min(1)] private int introEnemyCount = 3;

        [Header("First enemy")]
        [SerializeField] private EEnemyType firstEnemyType = EEnemyType.Grunt;
        [SerializeField] private int firstEnemyExtraHealth;
        [SerializeField, Min(1)] private int firstEnemyExperience = 34;
        [SerializeField, Range(0.5f, 1f)] private float firstEnemyRangeRatio = 0.88f;

        [Header("First level-up choices")]
        [SerializeField] private List<TowerBuffDefinition> firstBuffChoices = new();
        [SerializeField] private TowerBuffDefinition highlightedLegendaryBuff;

        [Header("Presentation")]
        [SerializeField, Range(0.01f, 1f)] private float explanationTimeScale = 0.1f;

        [Header("Ultimate showcase")]
        [SerializeField] private EEnemyType showcaseEnemyType = EEnemyType.Grunt;
        [SerializeField, Min(1)] private int showcaseEnemyCount = 14;
        [SerializeField, Min(0)] private int showcaseEnemyExtraHealth = 120;
        [SerializeField, Min(0f)] private float showcaseEnemyExtraSpeed;
        [SerializeField, Min(0.1f)] private float multishotPreviewSeconds = 4.8f;
        [SerializeField, Min(1f)] private float ultimateDamageMultiplier = 6f;
        [SerializeField, Min(0f)] private float celebrationSeconds = 2f;

        [Header("Side tower introduction")]
        [SerializeField] private SideTowerDefinition tutorialSideTower;
        [SerializeField] private EEnemyType sideTowerWaveEnemyType = EEnemyType.Armored;
        [SerializeField, Min(1)] private int sideTowerWaveEnemyCount = 6;
        [SerializeField, Min(0)] private int sideTowerWaveExtraHealth = 40;

        public bool Enabled => enabled;
        public int Version => version;
        public bool ForceInEditor => forceInEditor;
        public int LevelIndex => levelIndex;
        public int IntroEnemyCount => introEnemyCount;
        public EEnemyType FirstEnemyType => firstEnemyType;
        public int FirstEnemyExtraHealth => firstEnemyExtraHealth;
        public int FirstEnemyExperience => firstEnemyExperience;
        public float FirstEnemyRangeRatio => firstEnemyRangeRatio;
        public IReadOnlyList<TowerBuffDefinition> FirstBuffChoices => firstBuffChoices;
        public TowerBuffDefinition HighlightedLegendaryBuff => highlightedLegendaryBuff;
        public float ExplanationTimeScale => explanationTimeScale;
        public EEnemyType ShowcaseEnemyType => showcaseEnemyType;
        public int ShowcaseEnemyCount => showcaseEnemyCount;
        public int ShowcaseEnemyExtraHealth => showcaseEnemyExtraHealth;
        public float ShowcaseEnemyExtraSpeed => showcaseEnemyExtraSpeed;
        public float MultishotPreviewSeconds => multishotPreviewSeconds;
        public float UltimateDamageMultiplier => ultimateDamageMultiplier;
        public float CelebrationSeconds => celebrationSeconds;
        public SideTowerDefinition TutorialSideTower => tutorialSideTower;
        public EEnemyType SideTowerWaveEnemyType => sideTowerWaveEnemyType;
        public int SideTowerWaveEnemyCount => sideTowerWaveEnemyCount;
        public int SideTowerWaveExtraHealth => sideTowerWaveExtraHealth;

        public bool HasValidBuffChoices()
        {
            return firstBuffChoices != null
                   && firstBuffChoices.Count == 3
                   && highlightedLegendaryBuff != null
                   && firstBuffChoices.Contains(highlightedLegendaryBuff)
                   && tutorialSideTower != null;
        }
    }
}
