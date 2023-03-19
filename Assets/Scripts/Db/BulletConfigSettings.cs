using UnityEngine;
using Views.Impl;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(BulletConfigSettings),
        fileName = nameof(BulletConfigSettings))]
    public class BulletConfigSettings : ScriptableObject
    {
        [SerializeField] private BulletView prefabViewBullet;
        [SerializeField] private float speedMoving = 2f;
        
        public BulletView PrefabViewBullet => prefabViewBullet;
        public float SpeedMoving => speedMoving;
    }
}