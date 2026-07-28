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
        private IDisposable _cycleObserver;
        private bool _initialized;
        private int _lastTotalLevel;

        public void SetCycledProgress(int totalLevel, int cycleLength, bool animate = true)
        {
            cycleLength = Mathf.Max(1, cycleLength);

            var rank = totalLevel / cycleLength;
            var levelInRank = totalLevel % cycleLength;
            var completedCycle = totalLevel > 0 && levelInRank == 0 && totalLevel > _lastTotalLevel;

            _cycleObserver?.Dispose();

            if (completedCycle && animate && _initialized)
            {
                _cycleObserver = Observable.FromCoroutine(() => AnimateCycleComplete(cycleLength, rank)).Subscribe();
            }
            else
            {
                SetProgress(levelInRank, cycleLength, rank, animate);
            }

            _lastTotalLevel = totalLevel;
        }

        public void SetProgress(int current, int max, bool animate = true)
        {
            SetProgress(current, max, current / Mathf.Max(1, max), animate);
        }

        private void SetProgress(int current, int max, int rank, bool animate = true)
        {
            var target = max > 0 ? Mathf.Clamp01((float) current / max) : 0f;

            if (label != null)
                label.text = $"{current}/{max} R{rank + 1}";

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

        private IEnumerator AnimateCycleComplete(int cycleLength, int nextRank)
        {
            if (label != null)
                label.text = $"{cycleLength}/{cycleLength} R{nextRank}";

            _fillObserver?.Dispose();
            yield return AnimateFill(1f);

            _punchObserver?.Dispose();
            _punchObserver = Observable.FromCoroutine(PunchScale).Subscribe();

            yield return new WaitForSeconds(0.12f);

            if (fill != null)
                fill.fillAmount = 0f;

            if (label != null)
                label.text = $"0/{cycleLength} R{nextRank + 1}";
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
