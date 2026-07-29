using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Panels
{
    /// <summary>
    /// Вступительная анимация экрана победы плюс постоянные «живые» петли (дыхание свечения,
    /// пульс кнопки Next, покачивание звёзд).
    ///
    /// Запускается из OnEnable, а не по GameWinSignal: панель включает PanelsHandler, и порядок
    /// подписок PanelsHandler/WinPanelView на сигнал не определён — OnEnable же гарантированно
    /// приходит после SetActive(true). Сколько звёзд заработано, читаем не сразу, а прямо перед
    /// показом каждой звезды: к этому моменту WinPanelView уже успел проставить им activeSelf.
    ///
    /// Всё время — unscaledDeltaTime: на победе игра не встаёт на паузу, но панель не должна зависеть
    /// от игрового timeScale (тот же подход, что в фейде луз-панели у PanelsHandler).
    /// </summary>
    public class WinPanelAnimator : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Полупрозрачный фон-скрим (Image на корне панели). Плавно проявляется.")]
        [SerializeField] private Image scrim;
        [Tooltip("Рамка попапа — выезжает с overshoot-масштабом.")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform titleRibbon;
        [SerializeField] private RectTransform titleLabel;
        [SerializeField] private RectTransform starsGlow;
        [Tooltip("Заработанные звёзды в порядке слева-направо. Их activeSelf выставляет WinPanelView.")]
        [SerializeField] private RectTransform[] stars;
        [SerializeField] private RectTransform nextButton;
        [SerializeField] private RectTransform menuButton;

        [Header("Искры")]
        [Tooltip("Спрайт для салюта из искр. Если не задан — искр просто не будет, без ошибок.")]
        [SerializeField] private Sprite sparkleSprite;
        [SerializeField, Min(0)] private int sparkleCount = 16;
        [SerializeField, Min(0f)] private float sparkleLifetime = 1.25f;
        [SerializeField, Min(0f)] private float sparkleSpeed = 900f;

        [Header("Тайминги вступления")]
        [SerializeField, Min(0f)] private float scrimFade = 0.25f;
        [SerializeField, Min(0f)] private float panelPop = 0.42f;
        [SerializeField, Min(0f)] private float ribbonDelay = 0.18f;
        [SerializeField, Min(0f)] private float ribbonDrop = 0.5f;
        [SerializeField, Min(0f)] private float starsDelay = 0.62f;
        [SerializeField, Min(0f)] private float starStagger = 0.16f;
        [SerializeField, Min(0f)] private float starPop = 0.4f;
        [SerializeField, Min(0f)] private float buttonsDelay = 1.05f;

        // Стартовые значения, чтобы каждое переоткрытие панели начиналось с чистого листа
        // (панель переиспользуется: PanelsHandler гасит и включает её заново).
        private Vector2 _ribbonHome, _titleHome, _nextHome, _menuHome;
        private Vector3 _glowHome = Vector3.one;
        private readonly Vector3[] _starHomeScale = new Vector3[8];
        private readonly Quaternion[] _starHomeRot = new Quaternion[8];
        private readonly Vector2[] _starHomePos = new Vector2[8];
        private bool _homeCaptured;

        private CanvasGroup _panelGroup;
        private float _scrimAlpha = 1f;
        private Transform _sparkleRoot;
        private readonly List<Sparkle> _sparkles = new();

        private struct Sparkle
        {
            public RectTransform Rect;
            public Image Image;
            public Vector2 Velocity;
            public float Age;
            public float Spin;
        }

        private void Awake()
        {
            CaptureHome();
        }

        private void CaptureHome()
        {
            if (_homeCaptured)
                return;

            if (titleRibbon != null) _ribbonHome = titleRibbon.anchoredPosition;
            if (titleLabel != null) _titleHome = titleLabel.anchoredPosition;
            if (nextButton != null) _nextHome = nextButton.anchoredPosition;
            if (menuButton != null) _menuHome = menuButton.anchoredPosition;
            if (starsGlow != null) _glowHome = starsGlow.localScale;
            if (scrim != null) _scrimAlpha = scrim.color.a;

            if (stars != null)
                for (var i = 0; i < stars.Length && i < _starHomeScale.Length; i++)
                {
                    if (stars[i] == null) continue;
                    _starHomeScale[i] = stars[i].localScale;
                    _starHomeRot[i] = stars[i].localRotation;
                    _starHomePos[i] = stars[i].anchoredPosition;
                }

            if (panel != null)
            {
                _panelGroup = panel.GetComponent<CanvasGroup>();
                if (_panelGroup == null)
                    _panelGroup = panel.gameObject.AddComponent<CanvasGroup>();
            }

            _homeCaptured = true;
        }

        private void OnEnable()
        {
            CaptureHome();
            ClearSparkles();
            StopAllCoroutines();

            StartCoroutine(PlayIntro());
            StartCoroutine(LoopGlowBreath());
            StartCoroutine(LoopNextPulse());
            StartCoroutine(LoopStarsBob());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ClearSparkles();
        }

        private IEnumerator PlayIntro()
        {
            // --- исходное состояние
            if (scrim != null) SetAlpha(scrim, 0f);
            if (_panelGroup != null) _panelGroup.alpha = 0f;
            if (panel != null) panel.localScale = Vector3.one * 0.72f;
            if (titleRibbon != null) titleRibbon.anchoredPosition = _ribbonHome + new Vector2(0f, 260f);
            if (titleLabel != null) titleLabel.anchoredPosition = _titleHome + new Vector2(0f, 260f);
            if (nextButton != null) nextButton.anchoredPosition = _nextHome + new Vector2(0f, -70f);
            if (menuButton != null) menuButton.anchoredPosition = _menuHome + new Vector2(0f, -70f);
            SetButtonsAlpha(0f);
            HideStars();

            // Кадр ожидания: к следующему кадру WinPanelView уже обработал GameWinSignal,
            // поэтому activeSelf у звёзд соответствует реальному результату.
            yield return null;

            StartCoroutine(FadeScrim());
            StartCoroutine(PopPanel());
            yield return WaitUnscaled(ribbonDelay);

            StartCoroutine(DropRibbon());
            yield return WaitUnscaled(Mathf.Max(0f, starsDelay - ribbonDelay));

            for (var i = 0; i < (stars != null ? stars.Length : 0); i++)
            {
                StartCoroutine(PopStar(i));
                yield return WaitUnscaled(starStagger);
            }

            SpawnSparkleBurst();

            var toButtons = buttonsDelay - starsDelay - starStagger * (stars != null ? stars.Length : 0);
            if (toButtons > 0f) yield return WaitUnscaled(toButtons);

            StartCoroutine(SlideButton(nextButton, _nextHome, 0.32f));
            yield return WaitUnscaled(0.12f);
            StartCoroutine(SlideButton(menuButton, _menuHome, 0.32f));
        }

        private IEnumerator FadeScrim()
        {
            if (scrim == null) yield break;
            var t = 0f;
            while (t < scrimFade)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(scrim, Mathf.Lerp(0f, _scrimAlpha, EaseOutCubic(Mathf.Clamp01(t / scrimFade))));
                yield return null;
            }
            SetAlpha(scrim, _scrimAlpha);
        }

        private IEnumerator PopPanel()
        {
            if (panel == null) yield break;
            var t = 0f;
            while (t < panelPop)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / panelPop);
                panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.72f, 1f, EaseOutBack(k));
                if (_panelGroup != null) _panelGroup.alpha = Mathf.Clamp01(k / 0.45f);
                yield return null;
            }
            panel.localScale = Vector3.one;
            if (_panelGroup != null) _panelGroup.alpha = 1f;
        }

        private IEnumerator DropRibbon()
        {
            var t = 0f;
            var ribbonFrom = _ribbonHome + new Vector2(0f, 260f);
            var titleFrom = _titleHome + new Vector2(0f, 260f);
            while (t < ribbonDrop)
            {
                t += Time.unscaledDeltaTime;
                var e = EaseOutBack(Mathf.Clamp01(t / ribbonDrop));
                if (titleRibbon != null) titleRibbon.anchoredPosition = Vector2.LerpUnclamped(ribbonFrom, _ribbonHome, e);
                if (titleLabel != null) titleLabel.anchoredPosition = Vector2.LerpUnclamped(titleFrom, _titleHome, e);
                yield return null;
            }
            if (titleRibbon != null) titleRibbon.anchoredPosition = _ribbonHome;
            if (titleLabel != null) titleLabel.anchoredPosition = _titleHome;

            // короткий «удар» по заголовку, когда лента встала на место
            yield return PunchScale(titleLabel, 1.12f, 0.16f);
        }

        private IEnumerator PopStar(int index)
        {
            if (stars == null || index >= stars.Length || stars[index] == null) yield break;

            var star = stars[index];
            // Незаработанную звезду не анимируем: её погасил WinPanelView.
            if (!star.gameObject.activeSelf) yield break;

            var homeScale = _starHomeScale[index] == Vector3.zero ? Vector3.one : _starHomeScale[index];
            var homeRot = _starHomeRot[index];

            SpawnStarFlash(star);

            var t = 0f;
            while (t < starPop)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / starPop);
                star.localScale = homeScale * Mathf.LerpUnclamped(0.1f, 1f, EaseOutBack(k, 2.2f));
                // прилетает с проворотом — читается как «шлёп» печати
                star.localRotation = Quaternion.SlerpUnclamped(
                    homeRot * Quaternion.Euler(0f, 0f, -170f), homeRot, EaseOutCubic(k));
                yield return null;
            }
            star.localScale = homeScale;
            star.localRotation = homeRot;
        }

        private IEnumerator SlideButton(RectTransform target, Vector2 home, float duration)
        {
            if (target == null) yield break;
            var from = home + new Vector2(0f, -70f);
            var group = GetGroup(target);
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                target.anchoredPosition = Vector2.LerpUnclamped(from, home, EaseOutBack(k));
                if (group != null) group.alpha = k;
                yield return null;
            }
            target.anchoredPosition = home;
            if (group != null) group.alpha = 1f;
        }

        // ---------- постоянные петли ----------

        private IEnumerator LoopGlowBreath()
        {
            if (starsGlow == null) yield break;
            var img = starsGlow.GetComponent<Image>();
            var baseAlpha = img != null ? img.color.a : 0f;
            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                var w = (Mathf.Sin(t * 2.1f) + 1f) * 0.5f;
                starsGlow.localScale = _glowHome * Mathf.Lerp(0.94f, 1.09f, w);
                if (img != null) SetAlpha(img, Mathf.Lerp(baseAlpha * 0.65f, baseAlpha * 1.35f, w));
                yield return null;
            }
        }

        private IEnumerator LoopNextPulse()
        {
            if (nextButton == null) yield break;
            // ждём, пока кнопка приедет на место, иначе пульс будет спорить с выездом
            yield return WaitUnscaled(buttonsDelay + 0.4f);
            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                var s = 1f + Mathf.Sin(t * 3.4f) * 0.03f;
                nextButton.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        private IEnumerator LoopStarsBob()
        {
            if (stars == null) yield break;
            yield return WaitUnscaled(starsDelay + starPop + starStagger * stars.Length);
            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                for (var i = 0; i < stars.Length; i++)
                {
                    if (stars[i] == null || !stars[i].gameObject.activeSelf) continue;
                    // разные фазы, чтобы звёзды качались не синхронно
                    var off = Mathf.Sin(t * 1.7f + i * 1.1f) * 7f;
                    stars[i].anchoredPosition = _starHomePos[i] + new Vector2(0f, off);
                }
                yield return null;
            }
        }

        // ---------- искры ----------

        private void SpawnStarFlash(RectTransform star)
        {
            if (sparkleSprite == null) return;
            StartCoroutine(StarFlashRoutine(star));
        }

        private IEnumerator StarFlashRoutine(RectTransform star)
        {
            var go = new GameObject("StarFlash", typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(star.parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = star.anchoredPosition;
            rt.sizeDelta = star.sizeDelta * 1.1f;
            rt.SetSiblingIndex(Mathf.Max(0, star.GetSiblingIndex()));
            var img = go.GetComponent<Image>();
            img.sprite = sparkleSprite;
            img.raycastTarget = false;

            const float dur = 0.45f;
            var t = 0f;
            while (t < dur && go != null)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / dur);
                rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 2.6f, EaseOutCubic(k));
                img.color = new Color(1f, 1f, 1f, 1f - k);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        private void SpawnSparkleBurst()
        {
            if (sparkleSprite == null || sparkleCount <= 0 || panel == null) return;

            if (_sparkleRoot == null)
            {
                var root = new GameObject("Sparkles", typeof(RectTransform));
                var rrt = (RectTransform)root.transform;
                rrt.SetParent(panel, false);
                rrt.anchorMin = rrt.anchorMax = rrt.pivot = new Vector2(0.5f, 0.5f);
                rrt.anchoredPosition = starsGlow != null ? starsGlow.anchoredPosition : Vector2.zero;
                rrt.sizeDelta = Vector2.zero;
                _sparkleRoot = root.transform;
            }

            for (var i = 0; i < sparkleCount; i++)
            {
                var go = new GameObject("Sparkle", typeof(RectTransform), typeof(Image));
                var rt = (RectTransform)go.transform;
                rt.SetParent(_sparkleRoot, false);
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                var size = Random.Range(26f, 54f);
                rt.sizeDelta = new Vector2(size, size);

                var img = go.GetComponent<Image>();
                img.sprite = sparkleSprite;
                img.raycastTarget = false;

                var angle = Random.Range(0f, Mathf.PI * 2f);
                _sparkles.Add(new Sparkle
                {
                    Rect = rt,
                    Image = img,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(sparkleSpeed * 0.35f, sparkleSpeed),
                    Age = 0f,
                    Spin = Random.Range(-220f, 220f)
                });
            }
        }

        private void Update()
        {
            if (_sparkles.Count == 0) return;

            var dt = Time.unscaledDeltaTime;
            for (var i = _sparkles.Count - 1; i >= 0; i--)
            {
                var s = _sparkles[i];
                if (s.Rect == null) { _sparkles.RemoveAt(i); continue; }

                s.Age += dt;
                var k = Mathf.Clamp01(s.Age / Mathf.Max(0.01f, sparkleLifetime));
                s.Velocity += new Vector2(0f, -1500f) * dt;          // немного гравитации
                s.Velocity *= 1f - Mathf.Clamp01(2.2f * dt);          // и торможение
                s.Rect.anchoredPosition += s.Velocity * dt;
                s.Rect.localRotation *= Quaternion.Euler(0f, 0f, s.Spin * dt);
                s.Rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.35f, k);
                if (s.Image != null) s.Image.color = new Color(1f, 1f, 1f, 1f - k);

                if (k >= 1f)
                {
                    if (s.Rect != null) Destroy(s.Rect.gameObject);
                    _sparkles.RemoveAt(i);
                    continue;
                }
                _sparkles[i] = s;
            }
        }

        private void ClearSparkles()
        {
            foreach (var s in _sparkles)
                if (s.Rect != null) Destroy(s.Rect.gameObject);
            _sparkles.Clear();
        }

        // ---------- вспомогательное ----------

        private void HideStars()
        {
            if (stars == null) return;
            for (var i = 0; i < stars.Length; i++)
                if (stars[i] != null) stars[i].localScale = Vector3.zero;
        }

        private void SetButtonsAlpha(float a)
        {
            var g1 = GetGroup(nextButton);
            if (g1 != null) g1.alpha = a;
            var g2 = GetGroup(menuButton);
            if (g2 != null) g2.alpha = a;
        }

        private static CanvasGroup GetGroup(RectTransform target)
        {
            if (target == null) return null;
            var g = target.GetComponent<CanvasGroup>();
            return g != null ? g : target.gameObject.AddComponent<CanvasGroup>();
        }

        private IEnumerator PunchScale(RectTransform target, float peak, float duration)
        {
            if (target == null) yield break;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                // 0 -> peak -> 1
                var s = k < 0.5f
                    ? Mathf.Lerp(1f, peak, k / 0.5f)
                    : Mathf.Lerp(peak, 1f, (k - 0.5f) / 0.5f);
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
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

        private static void SetAlpha(Image img, float a)
        {
            var c = img.color;
            c.a = a;
            img.color = c;
        }

        private static float EaseOutCubic(float k) => 1f - Mathf.Pow(1f - k, 3f);

        private static float EaseOutBack(float k, float overshoot = 1.7f)
        {
            var c = overshoot + 1f;
            return 1f + c * Mathf.Pow(k - 1f, 3f) + overshoot * Mathf.Pow(k - 1f, 2f);
        }
    }
}
