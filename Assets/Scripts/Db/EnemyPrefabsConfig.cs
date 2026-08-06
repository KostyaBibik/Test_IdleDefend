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

        [Tooltip("Жёсткий потолок итоговой скорости врага (Speed + extraSpeed уровня). Башня стоит " +
                 "на месте и не может уклоняться, поэтому скорость — единственный рычаг сложности, " +
                 "против которого у игрока нет контрдействия: при радиусе атаки 4 враг на скорости " +
                 "0.9 находится в зоне поражения ~4.4 секунды, и это нижняя граница честного окна. " +
                 "Сложность выше по уровням набирается через HP, количество и состав волн.")]
        [SerializeField, Min(0.05f)] private float maxEnemySpeed = 0.9f;

        [Tooltip("Насколько сильно награда (монеты и опыт) следует за раздутым HP врага. " +
                 "1 = строго пропорционально: монет за врага ровно во столько же раз больше, во " +
                 "сколько он толще базового. Это выглядит честно, но HP внутри уровня растёт " +
                 "экспоненциально, поэтому и доход растёт экспоненциально — башня успевает " +
                 "перерасти волны в разы. Значения меньше 1 сглаживают: толстый враг всё ещё " +
                 "выгоднее, но не настолько, чтобы разогнать экономику вразнос. 0 = награда как " +
                 "раньше, только от базового значения EnemyDefinition.")]
        [SerializeField, Range(0f, 1f)] private float rewardHealthScaleExponent = 0.75f;

        [Tooltip("То же самое для опыта — но это ОТДЕЛЬНАЯ ручка, и она намеренно много меньше " +
                 "монетной. Монетам расти вместе с HP обязательно: цены апгрейдов тоже растут, и " +
                 "без этого поздние уровни не окупают прокачку. А уровни башни — фиксированная " +
                 "лестница из 31 ступени (TowerExperienceConfig.levelRequirements), она не " +
                 "растягивается. При общем множителе башня добирала 26-28 уровень за забег, все " +
                 "стаки баффов упирались в потолок, и выбор из трёх карточек переставал быть " +
                 "выбором. 0 = опыт строго по EnemyDefinition, без учёта раздутого HP.")]
        [SerializeField, Range(0f, 1f)] private float experienceHealthScaleExponent = 0.2f;

        [Space] [SerializeField] private List<EnemyDefinition> definitions;

        public float MinSpawnDelay => minSpawnDelay;
        public float MaxSpawnDelay => maxSpawnDelay;
        public float MaxEnemySpeed => maxEnemySpeed;
        public float RewardHealthScaleExponent => rewardHealthScaleExponent;
        public float ExperienceHealthScaleExponent => experienceHealthScaleExponent;
        public int CountPrefabs => definitions.Count;
        public IReadOnlyList<EnemyDefinition> Definitions => definitions;

        public EnemyDefinition GetPrefab(EEnemyType type)
        {
            var definition = definitions.FirstOrDefault(d => d.Type == type);
            if (definition == null)
                throw new Exception($"[EnemyPrefabsConfig] Can't find prefab with type: {type}");

            return definition;
        }
    }
}
