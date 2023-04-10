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
        [SerializeField] private int attackValue = 50;
        [SerializeField] private int startHealthCount = 1;
        [SerializeField] private int maxHealthCounts = 3;
        
        public TowerView PrefabViewTower => prefabViewTower;
        public TowerHealthView TowerHealthView => towerHealthView;
        public float RangeAttack => rangeAttack;
        public float AttackSpeed => attackSpeed;
        public int AttackValue => attackValue;
        public int StartHealthCount => startHealthCount;
        public int MaxHealthCounts => maxHealthCounts;
    }
}