using UI.Views.Game;
using UnityEngine;
using Views.Impl;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(TowerConfigSettings),
        fileName = nameof(TowerConfigSettings))]
    public class TowerConfigSettings : ScriptableObject
    {
        [SerializeField] private TowerView prefabViewTower;
        [SerializeField] private TowerHealthView towerHealthView;
        
        [Space, Header("Start values")] 
        [SerializeField] private float rangeAttack;
        [SerializeField] private float attackSpeed = .5f;
        [SerializeField] private int attackDamage = 50;
        [SerializeField] private int startHealthCount = 1;
        [Space, Header("Max values")] 
        [SerializeField] private int maxHealthCounts = 3;
        [SerializeField] private int maxAttackDamage = 200;
        [SerializeField] private float maxAttackSpeed = 3;
        [SerializeField] private int maxRangeAttack = 5;
        
        public TowerView PrefabViewTower => prefabViewTower;
        public TowerHealthView TowerHealthView => towerHealthView;
        public float RangeAttack => rangeAttack;
        public float AttackSpeed => attackSpeed;
        public int AttackDamage => attackDamage;
        public int StartHealthCount => startHealthCount;
        public int MaxHealthCounts => maxHealthCounts;
        public int MaxAttackDamage => maxAttackDamage;
        public float MaxAttackSpeed => maxAttackSpeed;
        public int MaxRangeAttack => maxRangeAttack;
    }
}