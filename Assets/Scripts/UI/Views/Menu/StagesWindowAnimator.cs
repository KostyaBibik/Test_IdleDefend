using System.Collections;
using System.Collections.Generic;
using UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Menu
{
    /// <summary>
    /// Оживляет окно этапов: вступление панелей, всплытие узлов карты, постоянный пульс текущего
    /// уровня и автопрокрутку от начала карты к тому уровню, на котором стоит игрок.
    ///
    /// Карта рассчитана на большое число уровней, поэтому анимируются только узлы, попадающие
    /// в видимую область: иначе на сотне уровней запускалась бы сотня корутин ради того,
    /// чего никто не увидит.
    /// </summary>
    public class StagesWindowAnimator : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private LevelPathBuilder pathBuilder;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [Tooltip("Область прокрутки карты: получает лёгкое масштабирование при открытии.")]
        [SerializeField] private RectTransform levelsContainer;

        [Header("Вступление")]
        [SerializeField, Min(0.01f)] private float barsSlide = 0.34f;
        [SerializeField] private float barsOffset = 160f;
        [SerializeField, Min(0f)] private float nodesDelay = 0.12f;
        [SerializeField, Min(0f)] private float nodeStagger = 0.05f;
        [SerializeField, Min(0.01f)] private float nodePop = 0.32f;

        [Header("Автопрокрутка")]
        [Tooltip("Сколько ждать после вступления, прежде чем поехать к текущему уровню.")]
        [SerializeField, Min(0f)] private float autoScrollDelay = 0.35f;
        [Tooltip("Длительность прокрутки на один экран карты. Полное время растёт с расстоянием, " +
                 "но упирается в maxAutoScrollDuration.")]
        [SerializeField, Min(0.01f)] private float autoScrollPerScreen = 0.55f;
        [SerializeField, Min(0.1f)] private float maxAutoScrollDuration = 1.8f;

        [Header("Петли")]
        [SerializeField] private float currentNodePulse = 0.06f;
        [SerializeField] private float currentNodePulseSpeed = 3f;

        // Автопрокрутка нужна, когда игроку есть что показать: первый показ за запуск игры или
        // открылся новый уровень. Повторный вход в то же состояние сразу показывает нужное место
        // без поездки — иначе она бы раздражала при каждом заходе.
        private static int _lastAutoScrolledIndex = -1;

        private readonly List<Coroutine> _running = new();
        private bool _pathReady;

        // Сколько длится всплытие узлов целиком — зависит от того, сколько их влезло на экран.
        private float _nodesIntroDuration;

        private void OnEnable()
        {
            StopAll();
            Track(StartCoroutine(ShowRoutine()));
        }

        private void OnDisable()
        {
            StopAll();
            ResetToHome();
        }

        private IEnumerator ShowRoutine()
        {
            // Карту строит Menu.Start(); при самом первом показе окна она может быть ещё не готова.
            yield return WaitForPath();

            var targetIndex = pathBuilder != null ? pathBuilder.UnlockedIndex : -1;
            var needsAutoScroll = targetIndex >= 0 && targetIndex != _lastAutoScrolledIndex;
            var scroll = pathBuilder != null ? pathBuilder.ScrollRect : null;

            if (scroll != null)
            {
                // Поездка всегда начинается с начала карты, а без неё сразу показываем нужное место.
                scroll.StopMovement();
                scroll.verticalNormalizedPosition = needsAutoScroll
                    ? 0f
                    : pathBuilder.GetNormalizedPositionForLevel(Mathf.Max(targetIndex, 0));
            }

            // Прокрутку ScrollRect применяет не сразу — без этого видимость узлов считалась бы
            // по позиции карты на прошлом кадре.
            Canvas.ForceUpdateCanvases();

            PlayBars();
            Track(StartCoroutine(PopVisibleNodes()));

            if (!needsAutoScroll || scroll == null)
            {
                // Без поездки пульсу нечего ждать, кроме всплытия узлов.
                yield return WaitUnscaled(_nodesIntroDuration);
            }
            else
            {
                yield return WaitUnscaled(autoScrollDelay);
                yield return AutoScrollRoutine(scroll, pathBuilder.GetNormalizedPositionForLevel(targetIndex));

                var targetNode = targetIndex < pathBuilder.Nodes.Count ? pathBuilder.Nodes[targetIndex] : null;
                if (targetNode != null)
                    yield return PunchRoutine((RectTransform)targetNode.transform);
            }

            // Индекс отмечаем только здесь, когда показ действительно состоялся. Окно активно
            // в сцене на старте и получает OnEnable ещё до того, как Menu построит карту; отметка
            // в начале сожгла бы поездку на этом прерванном показе.
            if (targetIndex >= 0)
                _lastAutoScrolledIndex = targetIndex;

            // Пульс запускаем последним: он пишет в тот же localScale, что всплытие и приветственный
            // отскок, и запущенный раньше срока перебивал бы их.
            Track(StartCoroutine(LoopCurrentNodePulse()));
        }

        private IEnumerator WaitForPath()
        {
            while (pathBuilder == null || pathBuilder.Nodes.Count == 0)
                yield return null;

            // Ещё кадр после включения окна: ScrollRect пересчитывает себя в своём OnEnable и
            // переписал бы позицию, выставленную в том же кадре.
            yield return null;

            // Позиции узлов и размер content нужны уже посчитанными: без этого автопрокрутка
            // целилась бы по пустой раскладке.
            Canvas.ForceUpdateCanvases();
            _pathReady = true;
        }

        private void PlayBars()
        {
            if (topBar != null)
                Track(StartCoroutine(SlideRoutine(topBar, new Vector2(0f, barsOffset))));

            if (bottomBar != null)
                Track(StartCoroutine(SlideRoutine(bottomBar, new Vector2(0f, -barsOffset))));

            if (levelsContainer != null)
                Track(StartCoroutine(PopRoutine(levelsContainer, 0.96f, barsSlide)));
        }

        private IEnumerator SlideRoutine(RectTransform target, Vector2 offset)
        {
            var home = target.anchoredPosition;
            var from = home + offset;
            var t = 0f;

            while (t < barsSlide)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / barsSlide);
                target.anchoredPosition = Vector2.LerpUnclamped(from, home, EaseOutCubic(k));
                yield return null;
            }

            target.anchoredPosition = home;
        }

        private IEnumerator PopVisibleNodes()
        {
            var visible = CollectVisibleNodes();
            _nodesIntroDuration = nodesDelay + Mathf.Max(visible.Count - 1, 0) * nodeStagger + nodePop;

            // Гасим до задержки: иначе узлы успели бы мигнуть в полный размер.
            foreach (var node in visible)
                node.localScale = Vector3.zero;

            yield return WaitUnscaled(nodesDelay);

            // Снизу вверх: карта читается от первого уровня к последнему.
            for (var i = 0; i < visible.Count; i++)
                Track(StartCoroutine(DelayedPop(visible[i], nodePop, i * nodeStagger)));
        }

        /// <summary>
        /// Узлы, попадающие в видимую область прокрутки. На карте из сотни уровней остальные
        /// анимировать бессмысленно — их всё равно не видно.
        /// </summary>
        private List<RectTransform> CollectVisibleNodes()
        {
            var result = new List<RectTransform>();
            if (pathBuilder == null || pathBuilder.ScrollRect == null)
                return result;

            var scroll = pathBuilder.ScrollRect;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            var corners = new Vector3[4];
            viewport.GetWorldCorners(corners);
            var viewMin = corners[0].y;
            var viewMax = corners[1].y;

            foreach (var node in pathBuilder.Nodes)
            {
                if (node == null)
                    continue;

                var rect = (RectTransform)node.transform;
                rect.GetWorldCorners(corners);

                if (corners[1].y >= viewMin && corners[0].y <= viewMax)
                    result.Add(rect);
            }

            return result;
        }

        private IEnumerator AutoScrollRoutine(ScrollRect scroll, float target)
        {
            var from = scroll.verticalNormalizedPosition;
            var distance = Mathf.Abs(target - from);
            if (distance <= 0.0001f)
                yield break;

            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            var content = scroll.content;
            var scrollableHeight = content != null ? content.rect.height - viewport.rect.height : 0f;
            var screens = viewport.rect.height > 0f ? distance * scrollableHeight / viewport.rect.height : 1f;

            // Время растёт с расстоянием, но с потолком: на длинной карте поездка не должна
            // превращаться в ожидание.
            var duration = Mathf.Min(autoScrollPerScreen * Mathf.Max(screens, 0.5f), maxAutoScrollDuration);
            var t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                scroll.verticalNormalizedPosition = Mathf.LerpUnclamped(from, target, EaseInOutCubic(k));
                yield return null;
            }

            scroll.verticalNormalizedPosition = target;
            scroll.StopMovement();
        }

        private IEnumerator LoopCurrentNodePulse()
        {
            if (pathBuilder == null || Mathf.Approximately(currentNodePulse, 0f))
                yield break;

            RectTransform current = null;
            foreach (var node in pathBuilder.Nodes)
            {
                if (node != null && node.IsNext)
                {
                    current = (RectTransform)node.transform;
                    break;
                }
            }

            if (current == null)
                yield break;

            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                var s = 1f + Mathf.Sin(t * currentNodePulseSpeed) * currentNodePulse;
                current.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private IEnumerator DelayedPop(RectTransform target, float duration, float delay)
        {
            if (delay > 0f)
                yield return WaitUnscaled(delay);

            yield return PopRoutine(target, 0f, duration);
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

        private IEnumerator PunchRoutine(RectTransform target)
        {
            const float duration = 0.36f;
            const float peak = 1.2f;
            var t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var s = k < 0.3f
                    ? Mathf.Lerp(1f, peak, k / 0.3f)
                    : Mathf.LerpUnclamped(peak, 1f, EaseOutBack((k - 0.3f) / 0.7f, 1.2f));
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        /// <summary>
        /// Окно переиспользуется при переходах, поэтому недоигранное состояние нужно снять:
        /// иначе следующий показ начался бы с узлов, сжатых в точку.
        /// </summary>
        private void ResetToHome()
        {
            if (!_pathReady || pathBuilder == null)
                return;

            foreach (var node in pathBuilder.Nodes)
                if (node != null)
                    node.transform.localScale = Vector3.one;
        }

        private void Track(Coroutine routine)
        {
            if (routine != null)
                _running.Add(routine);
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

        private static float EaseInOutCubic(float k)
        {
            return k < 0.5f
                ? 4f * k * k * k
                : 1f - Mathf.Pow(-2f * k + 2f, 3f) * 0.5f;
        }

        private static float EaseOutBack(float k, float overshoot = 1.7f)
        {
            var c = overshoot + 1f;
            return 1f + c * Mathf.Pow(k - 1f, 3f) + overshoot * Mathf.Pow(k - 1f, 2f);
        }
    }
}
