using UnityEngine;

namespace Views.Impl
{
    public class BulletView : MonoBehaviour, IEntityView
    {
        [HideInInspector] public EnemyView target;
        [HideInInspector] public int damage;
    }
}