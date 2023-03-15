using UnityEngine;

namespace Helpers
{
    public class SceneHandler : MonoBehaviour
    {
        [SerializeField] private Transform towerPos;
        public Transform TowerPos => towerPos;
    }
}