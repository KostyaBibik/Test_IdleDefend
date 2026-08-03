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

        [Tooltip("Сколько снарядов заранее создать и положить в пул при старте уровня " +
                 "(см. EntityPoolPrewarmSystem) - чтобы первые выстрелы не грузили Instantiate прямо во время геймплея.")]
        [SerializeField, Min(0)] private int poolPrewarmCount;

        public BulletView PrefabViewBullet => prefabViewBullet;
        public float SpeedMoving => speedMoving;
        public int PoolPrewarmCount => poolPrewarmCount;
    }
}