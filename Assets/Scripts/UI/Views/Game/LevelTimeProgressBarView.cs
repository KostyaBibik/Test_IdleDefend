using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Game
{
    /// <summary>
    /// Прогресс-бар времени уровня: заполняется по мере прохождения (без чисел),
    /// несёт 3 звезды-деления на порогах star1/star2/star3.
    /// </summary>
    public class LevelTimeProgressBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private LevelTimeProgressStarView[] stars;

        private void Awake()
        {
            fillImage.fillAmount = 0f;
            foreach (var star in stars)
                star.ResetState();
        }

        public void SetFillRatio(float ratio)
        {
            fillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        public void PositionStars(float star1Seconds, float star2Seconds, float star3Seconds)
        {
            if (star3Seconds <= 0f)
                return;

            PositionStar(0, star1Seconds / star3Seconds);
            PositionStar(1, star2Seconds / star3Seconds);
            PositionStar(2, 1f);
        }

        public void PlayStarReached(int starIndex)
        {
            if (starIndex < 0 || starIndex >= stars.Length)
                return;

            stars[starIndex].PlayFillAnimation();
        }

        private void PositionStar(int index, float fraction)
        {
            if (index < 0 || index >= stars.Length)
                return;

            var rt = (RectTransform) stars[index].transform;
            var clamped = Mathf.Clamp01(fraction);
            rt.anchorMin = new Vector2(clamped, 0.5f);
            rt.anchorMax = new Vector2(clamped, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
    }
}
