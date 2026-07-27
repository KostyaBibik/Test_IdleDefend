using System.Collections;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Game
{
    /// <summary>
    /// Одна звезда на прогресс-баре времени уровня. При достижении порога проигрывает
    /// подпрыгивание с апскейлом, закрашивается и остаётся закрашенной до конца уровня.
    /// </summary>
    public class LevelTimeProgressStarView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Sprite emptySprite;
        [SerializeField] private Sprite filledSprite;
        [SerializeField] private Image glow;
        [SerializeField] private Image ripple;
        [SerializeField] private ParticleSystem burstParticles;

        [Header("Тайминги анимации")]
        [SerializeField] private float bounceUpDuration = 0.15f;
        [SerializeField] private float bounceDownDuration = 0.2f;
        [SerializeField] private float peakScale = 1.4f;
        [SerializeField] private float glowFadeDuration = 0.6f;
        [SerializeField] private float rippleDuration = 0.4f;
        [SerializeField] private float rippleMaxScale = 3f;

        public bool IsFilled { get; private set; }

        private void Awake()
        {
            ResetState();
        }

        public void ResetState()
        {
            IsFilled = false;
            icon.sprite = emptySprite;
            transform.localScale = Vector3.one;
            SetImageAlpha(glow, 0f);
            SetImageAlpha(ripple, 0f);
        }

        public void PlayFillAnimation()
        {
            if (IsFilled)
                return;

            IsFilled = true;
            Observable.FromCoroutine(BounceAndFill).Subscribe();
            Observable.FromCoroutine(FadeGlow).Subscribe();
            Observable.FromCoroutine(PlayRipple).Subscribe();

            if (burstParticles != null)
                burstParticles.Play();
        }

        private IEnumerator BounceAndFill()
        {
            var start = Vector3.one;
            var peak = Vector3.one * peakScale;

            var t = 0f;
            while (t < bounceUpDuration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(start, peak, t / bounceUpDuration);
                yield return null;
            }

            transform.localScale = peak;
            icon.sprite = filledSprite;

            t = 0f;
            while (t < bounceDownDuration)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(peak, start, t / bounceDownDuration);
                yield return null;
            }

            transform.localScale = start;
        }

        private IEnumerator FadeGlow()
        {
            if (glow == null)
                yield break;

            SetImageAlpha(glow, 0.85f);

            var t = 0f;
            while (t < glowFadeDuration)
            {
                t += Time.deltaTime;
                SetImageAlpha(glow, Mathf.Lerp(0.85f, 0f, t / glowFadeDuration));
                yield return null;
            }

            SetImageAlpha(glow, 0f);
        }

        private IEnumerator PlayRipple()
        {
            if (ripple == null)
                yield break;

            ripple.transform.localScale = Vector3.one;
            SetImageAlpha(ripple, 0.8f);

            var t = 0f;
            while (t < rippleDuration)
            {
                t += Time.deltaTime;
                var ratio = t / rippleDuration;
                ripple.transform.localScale = Vector3.one * Mathf.Lerp(1f, rippleMaxScale, ratio);
                SetImageAlpha(ripple, Mathf.Lerp(0.8f, 0f, ratio));
                yield return null;
            }

            SetImageAlpha(ripple, 0f);
            ripple.transform.localScale = Vector3.one;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
