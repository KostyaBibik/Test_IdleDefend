using System.Collections.Generic;
using UI.Views;
using UI.Views.Panels;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Config/" + nameof(SideTowerCatalogConfig), fileName = nameof(SideTowerCatalogConfig))]
    public class SideTowerCatalogConfig : ScriptableObject
    {
        [SerializeField] private List<SideTowerDefinition> definitions;
        [SerializeField] private SideTowerSlotMarkerView slotMarkerPrefab;

        public IReadOnlyList<SideTowerDefinition> Definitions => definitions;
        public SideTowerSlotMarkerView SlotMarkerPrefab => slotMarkerPrefab;
    }
}
