using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class SideTowerSlotMarkerView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Canvas canvas;

        public Button Button => button;

        public void SetEventCamera(Camera camera)
        {
            if (canvas != null)
                canvas.worldCamera = camera;
        }
    }
}
