using UnityEngine;
using Views;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(EnemyPrefabsConfig),
        fileName = nameof(EnemyPrefabsConfig))]
    public class EnemyPrefabsConfig: ScriptableObject
    {
        [SerializeField] private EnemyView enemyView;
        public EnemyView EnemyView => enemyView;
    }
}