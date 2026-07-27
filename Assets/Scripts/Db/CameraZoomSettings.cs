using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(CameraZoomSettings), fileName = nameof(CameraZoomSettings))]
    public class CameraZoomSettings : ScriptableObject
    {
        [Tooltip("Запас поверх минимально необходимого радиуса обзора (1.2 = +20%)")]
        [SerializeField] private float radiusMargin = 1.2f;

        [Tooltip("Доля экрана снизу, занятая панелью апгрейдов (кадр сдвигается вверх на эту долю)")]
        [SerializeField, Range(0f, 0.6f)] private float bottomUiHeightRatio = 0.2497f;

        [Tooltip("Скорость плавного перехода зума/сдвига камеры к целевому значению")]
        [SerializeField] private float zoomLerpSpeed = 2.5f;

        [Tooltip("Защитный потолок отдаления (orthographicSize), чтобы не улететь в пустоту")]
        [SerializeField] private float maxOrthographicSize = 14f;

        public float RadiusMargin => radiusMargin;
        public float BottomUiHeightRatio => bottomUiHeightRatio;
        public float ZoomLerpSpeed => zoomLerpSpeed;
        public float MaxOrthographicSize => maxOrthographicSize;
    }
}
