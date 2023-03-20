using UnityEngine;
using Views.Impl;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(TowerConfigSettings),
        fileName = nameof(TowerConfigSettings))]
    public class TowerConfigSettings : ScriptableObject
    {
        [SerializeField] private TowerView prefabViewTower;

        [Space, Header("Start values")] 
        [SerializeField] private float rangeAttack;
        [SerializeField] private float attackSpeed = .5f;
        [SerializeField] private int attackValue = 50;
        
        public TowerView PrefabViewTower => prefabViewTower;
        public float RangeAttack => rangeAttack;
        public float AttackSpeed => attackSpeed;
        public int AttackValue => attackValue;
    }
}