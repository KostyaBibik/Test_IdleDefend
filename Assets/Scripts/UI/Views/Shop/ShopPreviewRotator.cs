using UnityEngine;

namespace UI.Views.Shop
{
    public class ShopPreviewRotator : MonoBehaviour
    {
        [SerializeField] private Vector3 rotationAxis = Vector3.up;
        [SerializeField] private float degreesPerSecond = 45f;

        private void Update()
        {
            transform.Rotate(rotationAxis, degreesPerSecond * Time.unscaledDeltaTime, Space.Self);
        }
    }
}
