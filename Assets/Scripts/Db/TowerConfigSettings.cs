using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(TowerConfigSettings),
        fileName = nameof(TowerConfigSettings))]
    public class TowerConfigSettings : ScriptableObject
    {
        [SerializeField] private GameObject prefabTower;
        public GameObject PrefabTower => prefabTower;
    }
}