using UnityEngine;

namespace Views.Impl
{
    public class SideTowerView : MonoBehaviour, IEntityView
    {
        [HideInInspector] public int attackDamage;
        [HideInInspector] public float attackSpeed;
        [HideInInspector] public float attackDistance;
        [HideInInspector] public float reloadRemaining;

        public bool isDestroyed { get; set; }
    }
}
