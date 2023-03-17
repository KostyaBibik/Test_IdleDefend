using UnityEngine;

namespace Views
{
    public class TowerView : MonoBehaviour
    {
        [SerializeField] private Transform sphere;

        public Transform Sphere => sphere;
    }
}