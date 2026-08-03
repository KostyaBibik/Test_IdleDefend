using System;
using System.Collections.Generic;
using Enums;
using UnityEngine;
using UnityEngine.Serialization;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(LevelDefinition),
        fileName = nameof(LevelDefinition))]
    public class LevelDefinition : ScriptableObject
    {
        [SerializeField] private int levelId;
        [Tooltip("Стартовые монеты внутри боевой сессии. Позволяет балансировать темп апгрейдов отдельно для каждого уровня.")]
        [SerializeField, Min(0)] private int startCoins = 120;
        [Tooltip("Враги уровня, сгруппированные по типу. Каждый враг настраивается отдельно на каждом из 3 этапов " +
                 "(старт-star1, star1-star2, star2-star3) — см. LevelEnemyDefinition. Редактируется через " +
                 "кастомный инспектор LevelDefinitionEditor.")]
        [SerializeField] private List<LevelEnemyDefinition> levelEnemies = new();

        [Tooltip("Эмеральды за ОДНУ звезду уровня. Полная зачистка даёт 3 звезды, то есть тройную " +
                 "величину. Награда выдаётся только за прирост звёзд относительно лучшего результата " +
                 "(см. LevelRewardService), поэтому перепрохождение не фармится.")]
        [FormerlySerializedAs("rewardEmeralds")]
        [SerializeField] private int rewardEmeraldsPerStar = 25;

        [Tooltip("Предмет магазина, который выдаётся бесплатно за первое прохождение этого уровня " +
                 "(башня, снаряд). Выдаётся один раз: повторное прохождение ничего не дублирует, " +
                 "потому что предмет уже во владении. Пусто — обычный уровень без подарка.")]
        [SerializeField] private ShopItemDefinition unlockRewardItem;

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
        public IReadOnlyList<LevelEnemyDefinition> LevelEnemies => levelEnemies;
        public int RewardEmeraldsPerStar => rewardEmeraldsPerStar;
        public ShopItemDefinition UnlockRewardItem => unlockRewardItem;

        /// <summary>
        /// Максимум звёзд уровня. Пороги те же, что рисует прогресс-бар боя.
        /// </summary>
        public const int MaxStars = 3;

        /// <summary>
        /// Сколько звёзд заслуживает забег, продержавшийся elapsedSeconds. Пороги совпадают
        /// с чекпоинтами прогресс-бара, поэтому игрок видит свои звёзды прямо во время боя.
        ///
        /// Полная зачистка уровня наступает не раньше star3Seconds (после этого момента враги
        /// уже не спавнятся), так что победа всегда даёт три звезды — отдельного случая для неё
        /// не нужно.
        /// </summary>
        public int GetStarsForElapsed(float elapsedSeconds)
        {
            if (elapsedSeconds >= star3Seconds)
                return 3;

            if (elapsedSeconds >= star2Seconds)
                return 2;

            if (elapsedSeconds >= star1Seconds)
                return 1;

            return 0;
        }
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

        /// <summary>
        /// Собирает записи спавна для этапа sectionIndex (0/1/2) из списка врагов уровня,
        /// пропуская врагов, у которых этот этап не включён (см. LevelEnemyDefinition.GetStage).
        /// </summary>
        public List<EnemySpawnEntryDefinition> GetSpawnEntries(int sectionIndex)
        {
            var result = new List<EnemySpawnEntryDefinition>();
            if (levelEnemies == null)
                return result;

            for (var i = 0; i < levelEnemies.Count; i++)
            {
                var enemyDef = levelEnemies[i];
                var stage = enemyDef?.GetStage(sectionIndex);
                if (stage == null || !stage.enabled)
                    continue;

                result.Add(new EnemySpawnEntryDefinition
                {
                    enemyType = enemyDef.enemyType,
                    spawnDelayMin = stage.spawnDelayMin,
                    spawnDelayMax = stage.spawnDelayMax,
                    extraHealth = stage.extraHealth,
                    extraSpeed = stage.extraSpeed,
                    endOfWaveHealthMultiplier = stage.endOfWaveHealthMultiplier,
                    finalSpawnLeadSecondsOverride = stage.finalSpawnLeadSecondsOverride
                });
            }

            return result;
        }

        /// <summary>
        /// Границы (начало, конец) секции спавна по её индексу. Нужны, чтобы понять,
        /// насколько далеко волна продвинулась (см. EnemySpawnInitializeSystem и
        /// EnemySpawnEntryDefinition.endOfWaveHealthMultiplier).
        /// </summary>
        public void GetSpawnSectionBounds(int sectionIndex, out float start, out float end)
        {
            switch (sectionIndex)
            {
                case 0:
                    start = 0f;
                    end = star1Seconds;
                    return;
                case 1:
                    start = star1Seconds;
                    end = star2Seconds;
                    return;
                case 2:
                    start = star2Seconds;
                    end = star3Seconds;
                    return;
                default:
                    start = 0f;
                    end = 0f;
                    return;
            }
        }
    }

    /// <summary>
    /// Настройки одного типа врага на уровне. Враг может участвовать в любом подмножестве
    /// из 3 этапов уровня (старт-star1, star1-star2, star2-star3) — для каждого этапа
    /// настройки включаются/выключаются и хранятся отдельно.
    /// </summary>
    [Serializable]
    public class LevelEnemyDefinition
    {
        public EEnemyType enemyType;

        [SerializeField] private EnemyStageSettings stage1 = new();
        [SerializeField] private EnemyStageSettings stage2 = new();
        [SerializeField] private EnemyStageSettings stage3 = new();

        public EnemyStageSettings GetStage(int stageIndex)
        {
            return stageIndex switch
            {
                0 => stage1,
                1 => stage2,
                2 => stage3,
                _ => null
            };
        }
    }

    /// <summary>
    /// Настройки спавна врага в рамках одного этапа уровня. enabled == false означает,
    /// что на этом этапе враг не спавнится вовсе.
    /// </summary>
    [Serializable]
    public class EnemyStageSettings
    {
        public bool enabled;

        [Min(0.05f)] public float spawnDelayMin = 1f;
        [Min(0.05f)] public float spawnDelayMax = 1f;

        public int extraHealth;
        public float extraSpeed;

        [Min(1f)] public float endOfWaveHealthMultiplier = 1f;

        [Min(-1f)] public float finalSpawnLeadSecondsOverride = -1f;
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

        [Header("Рост HP внутри волны")]
        [Tooltip("Во сколько раз вырастет HP этого потока врагов к концу волны относительно её начала. " +
                 "1 = без изменений, 1.5 = к концу волны HP на 50% больше, чем было в начале. Рост линейный. " +
                 "Важно: 'начало волны' - это не всегда база (extraHealth). Если враг того же типа уже " +
                 "рос в предыдущей волне, следующая волна продолжает рост с той отметки, на которой " +
                 "предыдущая закончилась (чтобы не было провала HP на границе волн), и уже от неё " +
                 "откладывает свои +50%.")]
        [Min(1f)] public float endOfWaveHealthMultiplier = 1f;

        [Header("Финал уровня")]
        [Tooltip("За сколько секунд до конца уровня прекратить этот поток в финальной секции. -1 = значение типа врага, 0 = спавнить до конца.")]
        [Min(-1f)] public float finalSpawnLeadSecondsOverride = -1f;
    }
}
