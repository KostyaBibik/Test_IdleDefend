using UnityEngine;
using Views;

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
        
        public TowerView PrefabViewTower => prefabViewTower;
        public float RangeAttack => rangeAttack;
        public float AttackSpeed => attackSpeed;
    }
}