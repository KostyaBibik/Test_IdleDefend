using UI.Views.Game;
using UI.Views.Panels;
using UnityEngine;

namespace Helpers
{
    public class SceneHandler : MonoBehaviour
    {
        [SerializeField] private Transform towerPos;
        [SerializeField] private RectTransform parentForUiEffects;
        [SerializeField] private Transform[] sideTowerSlotMarkers;
        [SerializeField] private SideTowerPickerView sideTowerPickerView;
        [SerializeField] private LevelTimeProgressBarView levelTimeProgressBarView;
        public Transform TowerPos => towerPos;
        public RectTransform ParentForUiEffects => parentForUiEffects;
        public Transform[] SideTowerSlotMarkers => sideTowerSlotMarkers;
        public SideTowerPickerView SideTowerPickerView => sideTowerPickerView;
        public LevelTimeProgressBarView LevelTimeProgressBarView => levelTimeProgressBarView;

        private void OnDestroy()
        {
            Destroy(gameObject);
        }
    }
}
