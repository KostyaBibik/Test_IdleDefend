using UnityEngine;

namespace Views
{
    public class TowerView : MonoBehaviour
    {
        [SerializeField] private Transform sphere;

        [HideInInspector] public float attackDistance;
        [HideInInspector] public float attackSpeed;
        
        public Transform Sphere => sphere;
    }
}