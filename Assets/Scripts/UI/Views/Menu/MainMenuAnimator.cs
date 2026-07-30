using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Menu
{
    /// <summary>
    /// Оживляет главный экран: вступление (титул, прогресс, кнопки), постоянные петли
    /// (дыхание титула, пульс главной кнопки) и отклик на нажатие кнопок.
    ///
    /// Запускается из OnEnable: MainWindow гасится и включается при переходах Меню/Магазин/Этапы,
    /// поэтому вступление проигрывается каждый раз при возврате на главный экран.
    ///
    /// Время — unscaledDeltaTime: в меню timeScale трогать никто не должен, но зависеть от него
    /// незачем (тот же подход, что в остальных аниматорах проекта).
    /// </summary>
    public class MainMenuAnimator : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform progressRow;
        [Tooltip("Главная кнопка — получает пульс, чтобы взгляд цеплялся за неё.")]
        [SerializeField] private RectTransform primaryButton;
        [SerializeField] private RectTransform secondaryButton;
        [SerializeField] private RectTransform currencyPill;
        [SerializeField] private RectTransform recordLabel;

        [Header("Тайминги")]
        [SerializeField, Min(0f)] private float titleDrop = 0.5f;
        [SerializeField, Min(0f)] private float pillDelay = 0.12f;
        [SerializeField, Min(0f)] private float progressDelay = 0.26f;
        [SerializeField, Min(0f)] private float buttonsDelay = 0.36f;
        [SerializeField, Min(0f)] private float buttonStagger = 0.1f;
        [SerializeField, Min(0f)] private float buttonSlide = 0.36f;
        [Tooltip("На сколько пикселей кнопки выезжают снизу.")]
        [SerializeField] private float buttonRise = 90f;

        [Header("Петли")]
        [SerializeField] private float titleBobPixels = 8f;
        [SerializeField] private float primaryPulse = 0.03f;

        private Vector2 _titleHome, _progressHome, _primaryHome, _secondaryHome, _pillHome, _recordHome;
        private bool _homeCaptured;
        private readonly List<Coroutine> _running = new();

        private void Awake()
        {
            CaptureHome();
            HookPressFeedback(primaryButton);
            HookPressFeedback(secondaryButton);
        }

        private void CaptureHome()
        {
            if (_homeCaptured)
                return;

            if (title != null) _titleHome = title.anchoredPosition;
            if (progressRow != null) _progressHome = progressRow.anchoredPosition;
            if (primaryButton != null) _primaryHome = primaryButton.anchoredPosition;
            if (secondaryButton != null) _secondaryHome = secondaryButton.anchoredPosition;
            if (currencyPill != null) _pillHome = currencyPill.anchoredPosition;
            if (recordLabel != null) _recordHome = recordLabel.anchoredPosition;

            _homeCaptured = true;
        }

        private void OnEnable()
        {
            CaptureHome();
            StopAll();
            Track(StartCoroutine(Intro()));
            Track(StartCoroutine(LoopTitleBob()));
            Track(StartCoroutine(LoopPrimaryPulse()));
        }

        private void OnDisable()
        {
            StopAll();
            ResetToHome();
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

        /// <summary>
        /// Экран переиспользуется при переходах, поэтому без сброса следующий вход начался бы
        /// с того состояния, на котором прошлый раз прервались.
        /// </summary>
        private void ResetToHome()
        {
            if (!_homeCaptured)
                return;

            Place(title, _titleHome, 1f);
            Place(progressRow, _progressHome, 1f);
            Place(primaryButton, _primaryHome, 1f);
            Place(secondaryButton, _secondaryHome, 1f);
            Place(currencyPill, _pillHome, 1f);
            Place(recordLabel, _recordHome, 1f);

            if (title != null) title.localScale = Vector3.one;
            if (primaryButton != null) primaryButton.localScale = Vector3.one;
            if (secondaryButton != null) secondaryButton.localScale = Vector3.one;
        }

        private static void Place(RectTransform target, Vector2 pos, float alpha)
        {
            if (target == null)
                return;

            target.anchoredPosition = pos;
            var group = GetGroup(target);
            if (group != null)
                group.alpha = alpha;
        }

        private IEnumerator Intro()
        {
            // исходное состояние
            Place(title, _titleHome + new Vector2(0f, 220f), 0f);
            Place(progressRow, _progressHome + new Vector2(0f, 40f), 0f);
            Place(currencyPill, _pillHome + new Vector2(0f, 90f), 0f);
            Place(recordLabel, _recordHome + new Vector2(0f, -60f), 0f);
            Place(primaryButton, _primaryHome + new Vector2(0f, -buttonRise), 0f);
            Place(secondaryButton, _secondaryHome + new Vector2(0f, -buttonRise), 0f);

            yield return null;

            Track(StartCoroutine(Slide(title, _titleHome + new Vector2(0f, 220f), _titleHome, titleDrop, back: true)));

            yield return WaitUnscaled(pillDelay);
            Track(StartCoroutine(Slide(currencyPill, _pillHome + new Vector2(0f, 90f), _pillHome, 0.3f, back: true)));
            Track(StartCoroutine(Slide(recordLabel, _recordHome + new Vector2(0f, -60f), _recordHome, 0.36f, back: false)));

            yield return WaitUnscaled(Mathf.Max(0f, progressDelay - pillDelay));
            Track(StartCoroutine(Slide(progressRow, _progressHome + new Vector2(0f, 40f), _progressHome, 0.32f, back: true)));

            yield return WaitUnscaled(Mathf.Max(0f, buttonsDelay - progressDelay));
            Track(StartCoroutine(Slide(primaryButton, _primaryHome + new Vector2(0f, -buttonRise), _primaryHome, buttonSlide, back: true)));

            yield return WaitUnscaled(buttonStagger);
            Track(StartCoroutine(Slide(secondaryButton, _secondaryHome + new Vector2(0f, -buttonRise), _secondaryHome, buttonSlide, back: true)));
        }

        private IEnumerator Slide(RectTransform target, Vector2 from, Vector2 to, float duration, bool back)
        {
            if (target == null)
                yield break;

            var group = GetGroup(target);
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var eased = back ? EaseOutBack(k) : EaseOutCubic(k);
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
                if (group != null)
                    group.alpha = Mathf.Clamp01(k / 0.5f);
                yield return null;
            }

            target.anchoredPosition = to;
            if (group != null)
                group.alpha = 1f;
        }

        private IEnumerator LoopTitleBob()
        {
            if (title == null || Mathf.Approximately(titleBobPixels, 0f))
                yield break;

            yield return WaitUnscaled(titleDrop + 0.1f);

            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                title.anchoredPosition = _titleHome + new Vector2(0f, Mathf.Sin(t * 1.3f) * titleBobPixels);
                yield return null;
            }
        }

        private IEnumerator LoopPrimaryPulse()
        {
            if (primaryButton == null || Mathf.Approximately(primaryPulse, 0f))
                yield break;

            // ждём, пока кнопка приедет, иначе пульс спорит с выездом
            yield return WaitUnscaled(buttonsDelay + buttonSlide + 0.1f);

            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                var s = 1f + Mathf.Sin(t * 3.2f) * primaryPulse;
                primaryButton.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        /// <summary>
        /// Короткий «клик» по кнопке. Вешается на onClick, а не на pointer-down: сцена всё равно
        /// сменится, и это единственный момент, когда отклик успевает прочитаться.
        /// </summary>
        private void HookPressFeedback(RectTransform target)
        {
            if (target == null)
                return;

            var button = target.GetComponent<Button>();
            if (button == null)
                return;

            button.onClick.AddListener(() =>
            {
                if (!gameObject.activeInHierarchy)
                    return;

                Track(StartCoroutine(Punch(target)));
            });
        }

        private IEnumerator Punch(RectTransform target)
        {
            const float duration = 0.16f;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var s = k < 0.4f
                    ? Mathf.Lerp(1f, 0.92f, k / 0.4f)
                    : Mathf.Lerp(0.92f, 1f, (k - 0.4f) / 0.6f);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private static CanvasGroup GetGroup(Component target)
        {
            if (target == null)
                return null;

            var group = target.GetComponent<CanvasGroup>();
            return group != null ? group : target.gameObject.AddComponent<CanvasGroup>();
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
