using UnityEngine;

namespace Views.Impl
{
    public class TowerView : MonoBehaviour, IEntityView
    {
        [SerializeField] private Transform sphere;

        [HideInInspector] public float ratioRange = 0.5f;
        [HideInInspector] public float attackDistance;
        [HideInInspector] public float attackSpeed;
        [HideInInspector] public int attackDamage;
        
        public Transform Sphere => sphere;
        public bool isDestroyed { get; set; }
    }
}