using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Game
{
    /// <summary>
    /// Единичный XP-орб. Чисто визуальный контейнер без собственной логики — весь полёт
    /// (разлёт/пауза/полёт к бару/прилёт) считает и применяет TowerExperienceOrbSystem,
    /// здесь только ссылки на компоненты, которые он двигает и переключает.
    /// </summary>
    public class TowerExperienceOrbView : MonoBehaviour
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private Image icon;

        [Tooltip("Точка подключения под будущий pop-эффект (и звук) прилёта — " +
                 "активируется системой в момент, когда орб долетает до бара, и плавно гаснет.")]
        [SerializeField] private GameObject arrivalGlow;
        [SerializeField] private Image arrivalGlowImage;

        // Орб переиспользуется через пул: чтобы вспышка прилёта каждый раз начинала фейд с
        // одного и того же авторского альфа-канала, а не с того, на чём остановилась в прошлый
        // раз, запоминаем её при первом Awake и умеем сбрасывать перед повторным использованием.
        private Color _arrivalGlowBaseColor;
        private bool _arrivalGlowColorCaptured;

        public RectTransform RectTransform => rectTransform;
        public Image Icon => icon;
        public GameObject ArrivalGlow => arrivalGlow;
        public Image ArrivalGlowImage => arrivalGlowImage;
        public Color ArrivalGlowBaseColor => _arrivalGlowBaseColor;

        private void Awake()
        {
            CaptureArrivalGlowBaseColor();
        }

        /// <summary>Возвращает вспышку прилёта к авторскому цвету/альфе перед новым вылетом орба.</summary>
        public void ResetArrivalGlow()
        {
            CaptureArrivalGlowBaseColor();

            if (arrivalGlowImage != null)
                arrivalGlowImage.color = _arrivalGlowBaseColor;
        }

        private void CaptureArrivalGlowBaseColor()
        {
            if (_arrivalGlowColorCaptured || arrivalGlowImage == null)
                return;

            _arrivalGlowBaseColor = arrivalGlowImage.color;
            _arrivalGlowColorCaptured = true;
        }
    }
}
