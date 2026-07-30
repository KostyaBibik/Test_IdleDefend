using System.Collections.Generic;
using Enums;
using UnityEngine;
using Views.Impl;
using Random = UnityEngine.Random;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(EnemyDefinition), fileName = nameof(EnemyDefinition))]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Тип")]
        [SerializeField] private EEnemyType type;

        [Header("Визуал")]
        [SerializeField] private EnemyView viewPrefab;
        [SerializeField] private List<ParticleSystem> killParticles;

        [Header("Характеристики")]
        [SerializeField] private int health = 100;
        [SerializeField] private float speed = 0.3f;
        [SerializeField] private int rewardCoins = 50;
        [SerializeField] private int experienceReward = 5;
        [SerializeField] private int damageToTower = 1;
        [SerializeField, Range(0f, 1f)] private float damageReduction;

        [Header("Финал уровня")]
        [Tooltip("За сколько секунд до конца уровня этот тип перестаёт спавниться в финальной секции.")]
        [SerializeField, Min(0f)] private float finalSpawnLeadSeconds = 4f;

        [Header("Движение")]
        [SerializeField] private EEnemyMovementType movementType = EEnemyMovementType.Linear;
        [SerializeField] private float orbitAngularSpeedDegPerSec = 60f;
        [SerializeField] private float orbitRadiusShrinkSpeed = 0.3f;

        [Header("Смерть")]
        [SerializeField] private EEnemyDeathBehavior onDeath = EEnemyDeathBehavior.None;
        [SerializeField] private EnemyDefinition splitChildType;
        [SerializeField] private int splitChildCount = 3;
        [SerializeField] private float deathDelay = 0.6f;

        public EEnemyType Type => type;
        public EnemyView ViewPrefab => viewPrefab;

        public int Health => health;
        public float Speed => speed;
        public int RewardCoins => rewardCoins;
        public int ExperienceReward => experienceReward;
        public int DamageToTower => damageToTower;
        public float DamageReduction => damageReduction;
        public float FinalSpawnLeadSeconds => finalSpawnLeadSeconds;

        public EEnemyMovementType MovementType => movementType;
        public float OrbitAngularSpeedDegPerSec => orbitAngularSpeedDegPerSec;
        public float OrbitRadiusShrinkSpeed => orbitRadiusShrinkSpeed;

        public EEnemyDeathBehavior OnDeath => onDeath;
        public EnemyDefinition SplitChildType => splitChildType;
        public int SplitChildCount => splitChildCount;
        public float DeathDelay => deathDelay;

        public ParticleSystem GetRandomParticle()
        {
            return killParticles[Random.Range(0, killParticles.Count)];
        }
    }
}
