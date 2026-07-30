using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Views.Shop
{
    /// <summary>
    /// Оживляет предбоевой экран выбора бустов: вступление панелей и строк, отклик на выбор буста,
    /// пульс кнопки старта и короткий финал при закрытии.
    ///
    /// Строки лежат в VerticalLayoutGroup — раскладка каждый кадр переписывает их позиции,
    /// поэтому анимируем только localScale и alpha.
    /// </summary>
    public class BoostSelectWindowAnimator : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private RectTransform page;
        [SerializeField] private RectTransform topBar;
        [Tooltip("Контейнер строк бустов: анимируются его активные дети.")]
        [SerializeField] private RectTransform itemsList;
        [SerializeField] private RectTransform startButton;

        [Header("Вступление")]
        [SerializeField, Min(0.01f)] private float pageFade = 0.24f;
        [SerializeField, Min(0.01f)] private float barSlide = 0.32f;
        [SerializeField] private float barOffset = 140f;
        [SerializeField, Min(0f)] private float rowsDelay = 0.1f;
        [SerializeField, Min(0f)] private float rowStagger = 0.07f;
        [SerializeField, Min(0.01f)] private float rowPop = 0.34f;
        [Tooltip("С какого масштаба строка разворачивается. Позицию не трогаем: строки лежат " +
                 "в VerticalLayoutGroup, и раскладка вернула бы любой сдвиг обратно.")]
        [SerializeField, Range(0.1f, 1f)] private float rowFromScale = 0.82f;
        [SerializeField, Min(0f)] private float buttonDelay = 0.34f;
        [SerializeField, Min(0.01f)] private float buttonPop = 0.36f;

        [Header("Финал")]
        [SerializeField, Min(0.01f)] private float outroDuration = 0.2f;

        [Header("Отклик и петли")]
        [SerializeField] private float togglePunch = 1.06f;
        [SerializeField, Min(0.01f)] private float togglePunchDuration = 0.24f;
        [SerializeField] private float startButtonPulse = 0.028f;
        [SerializeField] private float startButtonPulseSpeed = 3f;

        private readonly List<Coroutine> _running = new();
        private readonly List<RectTransform> _touchedRows = new();

        private CanvasGroup _pageGroup;
        private Coroutine _outro;

        /// <summary>
        /// Вступление. Вызывается из BoostSelectWindowView, а не из OnEnable: окно наполняется
        /// строками в Open уже после SetActive, и запуск по OnEnable ловил бы прошлый набор.
        /// </summary>
        public void PlayIntro()
        {
            if (!gameObject.activeInHierarchy)
                return;

            Stop(ref _outro);
            StopAll();
            _running.Add(StartCoroutine(IntroRoutine()));
        }

        /// <summary>Финал закрытия. onDone вызывается всегда, даже если окно уже неактивно.</summary>
        public void PlayOutro(Action onDone)
        {
            if (!gameObject.activeInHierarchy)
            {
                onDone?.Invoke();
                return;
            }

            StopAll();
            _outro = StartCoroutine(OutroRoutine(onDone));
        }

        /// <summary>Отклик на включение/выключение буста.</summary>
        public void PunchRow(Component row)
        {
            if (!gameObject.activeInHierarchy || row == null)
                return;

            _running.Add(StartCoroutine(PunchRoutine((RectTransform)row.transform, togglePunch, togglePunchDuration)));
        }

        private void OnDisable()
        {
            StopAll();
            Stop(ref _outro);
            ResetToHome();
        }

        private IEnumerator IntroRoutine()
        {
            var rows = CollectRows();

            // Гасим до задержек: иначе строки и кнопка мигнули бы в полный размер.
            foreach (var row in rows)
            {
                row.localScale = new Vector3(rowFromScale, rowFromScale, 1f);
                GetGroup(row).alpha = 0f;

                if (!_touchedRows.Contains(row))
                    _touchedRows.Add(row);
            }

            if (startButton != null)
                startButton.localScale = Vector3.zero;

            var group = GetPageGroup();
            if (group != null)
                group.alpha = 0f;

            yield return null;

            if (group != null)
                _running.Add(StartCoroutine(FadeRoutine(group, 0f, 1f, pageFade)));

            if (topBar != null)
                _running.Add(StartCoroutine(SlideRoutine(topBar, new Vector2(0f, barOffset), barSlide)));

            yield return WaitUnscaled(rowsDelay);

            for (var i = 0; i < rows.Count; i++)
                _running.Add(StartCoroutine(RowIntro(rows[i], i * rowStagger)));

            yield return WaitUnscaled(Mathf.Max(0f, buttonDelay - rowsDelay));

            if (startButton != null)
                _running.Add(StartCoroutine(PopRoutine(startButton, 0f, buttonPop)));

            // Пульс — после того как кнопка приехала, иначе они спорят за один и тот же масштаб.
            yield return WaitUnscaled(buttonPop);
            _running.Add(StartCoroutine(LoopStartButtonPulse()));
        }

        private IEnumerator RowIntro(RectTransform row, float delay)
        {
            if (delay > 0f)
                yield return WaitUnscaled(delay);

            var group = GetGroup(row);
            var t = 0f;

            while (t < rowPop)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / rowPop);

                var s = Mathf.LerpUnclamped(rowFromScale, 1f, EaseOutBack(k));
                row.localScale = new Vector3(s, s, 1f);
                group.alpha = Mathf.Clamp01(k / 0.5f);
                yield return null;
            }

            row.localScale = Vector3.one;
            group.alpha = 1f;
        }

        private IEnumerator OutroRoutine(Action onDone)
        {
            var group = GetPageGroup();
            var from = group != null ? group.alpha : 1f;
            var t = 0f;

            while (t < outroDuration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / outroDuration);

                if (group != null)
                    group.alpha = Mathf.Lerp(from, 0f, k);

                if (page != null)
                {
                    var s = Mathf.Lerp(1f, 0.94f, k);
                    page.localScale = new Vector3(s, s, 1f);
                }

                yield return null;
            }

            _outro = null;
            ResetToHome();
            onDone?.Invoke();
        }

        private IEnumerator LoopStartButtonPulse()
        {
            if (startButton == null || Mathf.Approximately(startButtonPulse, 0f))
                yield break;

            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                var s = 1f + Mathf.Sin(t * startButtonPulseSpeed) * startButtonPulse;
                startButton.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private IEnumerator PunchRoutine(RectTransform target, float peak, float duration)
        {
            if (target == null)
                yield break;

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var s = k < 0.35f
                    ? Mathf.Lerp(1f, peak, k / 0.35f)
                    : Mathf.LerpUnclamped(peak, 1f, EaseOutBack((k - 0.35f) / 0.65f, 1.2f));
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private IEnumerator PopRoutine(RectTransform target, float from, float duration)
        {
            if (target == null)
                yield break;

            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var s = Mathf.LerpUnclamped(from, 1f, EaseOutBack(k));
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private IEnumerator SlideRoutine(RectTransform target, Vector2 offset, float duration)
        {
            var home = target.anchoredPosition;
            var from = home + offset;
            var t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, home, EaseOutCubic(k));
                yield return null;
            }

            target.anchoredPosition = home;
        }

        private static IEnumerator FadeRoutine(CanvasGroup group, float from, float to, float duration)
        {
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }

            group.alpha = to;
        }

        private List<RectTransform> CollectRows()
        {
            var result = new List<RectTransform>();
            if (itemsList == null)
                return result;

            for (var i = 0; i < itemsList.childCount; i++)
            {
                if (itemsList.GetChild(i) is RectTransform row && row.gameObject.activeSelf)
                    result.Add(row);
            }

            return result;
        }

        /// <summary>
        /// Окно переиспользуется, поэтому недоигранное состояние надо снять: иначе следующее
        /// открытие началось бы с прозрачных строк и сжатой кнопки.
        /// </summary>
        private void ResetToHome()
        {
            if (page != null)
                page.localScale = Vector3.one;

            var group = GetPageGroup();
            if (group != null)
                group.alpha = 1f;

            if (startButton != null)
                startButton.localScale = Vector3.one;

            foreach (var row in _touchedRows)
            {
                if (row == null)
                    continue;

                row.localScale = Vector3.one;
                GetGroup(row).alpha = 1f;
            }

            _touchedRows.Clear();
        }

        private CanvasGroup GetPageGroup()
        {
            if (page == null)
                return null;

            if (_pageGroup == null)
                _pageGroup = GetGroup(page);

            return _pageGroup;
        }

        private static CanvasGroup GetGroup(Component target)
        {
            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
        }

        private void Stop(ref Coroutine routine)
        {
            if (routine != null)
                StopCoroutine(routine);

            routine = null;
        }

        private void StopAll()
        {
            foreach (var routine in _running)
                if (routine != null)
                    StopCoroutine(routine);

            _running.Clear();
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            var t = 0f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static float EaseOutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);

        private static float EaseOutBack(float k, float overshoot = 1.7f)
        {
            var c = overshoot + 1f;
            return 1f + c * Mathf.Pow(k - 1f, 3f) + overshoot * Mathf.Pow(k - 1f, 2f);
        }
    }
}
