using System;
using System.Collections;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Upgradable
{
    /// <summary>
    /// Сегментированный прогресс-бар уровня апгрейда (текущий/максимум): плавная анимация
    /// заполнения + лёгкий пружинный "punch" при повышении уровня.
    /// </summary>
    public class UpgradeProgressBarView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform punchTarget;
        [SerializeField] private float fillSpeed = 3f;
        [SerializeField] private float punchScale = 1.08f;
        [SerializeField] private float punchSpeed = 6f;

        private IDisposable _fillObserver;
        private IDisposable _punchObserver;
        private bool _initialized;

        public void SetProgress(int current, int max, bool animate = true)
        {
            var target = max > 0 ? Mathf.Clamp01((float) current / max) : 0f;

            if (label != null)
                label.text = $"{current}/{max}";

            if (!animate || !_initialized)
            {
                if (fill != null)
                    fill.fillAmount = target;
            }
            else
            {
                _fillObserver?.Dispose();
                _fillObserver = Observable.FromCoroutine(() => AnimateFill(target)).Subscribe();

                _punchObserver?.Dispose();
                _punchObserver = Observable.FromCoroutine(PunchScale).Subscribe();
            }

            _initialized = true;
        }

        private IEnumerator AnimateFill(float target)
        {
            if (fill == null)
                yield break;

            while (!Mathf.Approximately(fill.fillAmount, target))
            {
                fill.fillAmount = Mathf.MoveTowards(fill.fillAmount, target, fillSpeed * Time.deltaTime);
                yield return null;
            }

            fill.fillAmount = target;
        }

        private IEnumerator PunchScale()
        {
            if (punchTarget == null)
                yield break;

            var t = 0f;
            while (t < 1f)
            {
                t += punchSpeed * Time.deltaTime;
                var s = 1f + Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * (punchScale - 1f);
                punchTarget.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            punchTarget.localScale = Vector3.one;
        }
    }
}
