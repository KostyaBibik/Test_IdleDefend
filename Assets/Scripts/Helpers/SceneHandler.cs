using UnityEngine;

namespace Helpers
{
    public class SceneHandler : MonoBehaviour
    {
        [SerializeField] private Transform towerPos;
        [SerializeField] private RectTransform parentForUiEffects;
        public Transform TowerPos => towerPos;
        public RectTransform ParentForUiEffects => parentForUiEffects;
    }
}