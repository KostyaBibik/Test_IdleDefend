using UnityEngine;

namespace Views.Impl
{
    public class TowerView : MonoBehaviour, IEntityView
    {
        [SerializeField] private Transform sphere;

        /*[HideInInspector]*/ public float attackDistance;
        [HideInInspector] public float attackSpeed;
        [HideInInspector] public int attackValue;
        
        public Transform Sphere => sphere;
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position, attackDistance);
        }
    }
}