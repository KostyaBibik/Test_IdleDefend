using System.Collections;
using System.Collections.Generic;
using Services;
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

        [Header("Награда")]
        [Tooltip("Строка с иконкой гема и числом. Не назначена — награда просто не показывается.")]
        [SerializeField] private RectTransform rewardRow;
        [SerializeField] private TMPro.TMP_Text rewardLabel;
        [Tooltip("Спрайт летящего гема. Гемы вылетают из каждой заработанной звезды в строку награды.")]
        [SerializeField] private Sprite gemSprite;
        [SerializeField, Min(1)] private int gemsPerStar = 5;
        [SerializeField, Min(0.01f)] private float gemFlightDuration = 0.55f;
        [SerializeField, Min(0f)] private float gemFlightStagger = 0.05f;
        [Tooltip("Сколько крутится счётчик после прилёта последнего гема.")]
        [SerializeField, Min(0.01f)] private float counterDuration = 0.7f;

        [Header("Подарок за уровень")]
        [Tooltip("Карточка новой башни/снаряда. Не назначена — подарок просто не показывается.")]
        [SerializeField] private RectTransform unlockCard;
        [SerializeField] private Image unlockIcon;
        [SerializeField] private TMPro.TMP_Text unlockNameLabel;
        [Tooltip("Свечение под карточкой подарка — пульсирует, пока панель открыта.")]
        [SerializeField] private RectTransform unlockGlow;
        [SerializeField, Min(0f)] private float unlockDelay = 0.35f;
        [SerializeField, Min(0.01f)] private float unlockPop = 0.5f;
        [Tooltip("На сколько панель короче, когда подарка нет. Панель растёт вниз от неподвижного " +
                 "верхнего края, поэтому заголовок и звёзды при этом не двигаются.")]
        [SerializeField, Min(0f)] private float unlockBlockHeight = 190f;

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

        private LevelRewardResult _reward;
        private bool _rewardKnown;
        private Db.ShopItemDefinition _unlockedItem;

        // Исходная (максимальная) высота панели и текущие цели кнопок — зависят от того,
        // показываем ли мы блок подарка.
        private float _panelHomeHeight;
        private Vector2 _nextTarget, _menuTarget;

        // Летящие гемы создаются на лету, поэтому их надо прибрать вместе с панелью:
        // иначе прерванный полёт оставит иконку висеть на экране.
        private readonly List<GameObject> _spawnedGems = new();

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
            if (panel != null) _panelHomeHeight = panel.sizeDelta.y;
            _nextTarget = _nextHome;
            _menuTarget = _menuHome;
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
            ClearGems();
        }

        private void ClearGems()
        {
            foreach (var gem in _spawnedGems)
                if (gem != null)
                    Destroy(gem);

            _spawnedGems.Clear();
        }

        private IEnumerator PlayIntro()
        {
            // --- исходное состояние
            if (scrim != null) SetAlpha(scrim, 0f);
            if (_panelGroup != null) _panelGroup.alpha = 0f;
            if (panel != null) panel.localScale = Vector3.one * 0.72f;
            if (titleRibbon != null) titleRibbon.anchoredPosition = _ribbonHome + new Vector2(0f, 260f);
            if (titleLabel != null) titleLabel.anchoredPosition = _titleHome + new Vector2(0f, 260f);
            if (nextButton != null) nextButton.anchoredPosition = _nextTarget + new Vector2(0f, -70f);
            if (menuButton != null) menuButton.anchoredPosition = _menuTarget + new Vector2(0f, -70f);
            SetButtonsAlpha(0f);
            HideStars();

            // Карточку подарка гасим сразу, а не в UnlockRoutine: та отрабатывает только после
            // гемов, и до неё пустая карточка секунды висела бы на экране белым прямоугольником.
            if (unlockCard != null) unlockCard.gameObject.SetActive(false);
            if (rewardRow != null) rewardRow.gameObject.SetActive(false);
            ApplyPanelHeight();

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

            StartCoroutine(RewardAndUnlockRoutine());

            StartCoroutine(SlideButton(nextButton, _nextTarget, 0.32f));
            yield return WaitUnscaled(0.12f);
            StartCoroutine(SlideButton(menuButton, _menuTarget, 0.32f));
        }

        /// <summary>
        /// Награду сообщает WinPanelView по сигналу победы — до того, как панель включат.
        /// Проигрывается она уже из вступления, вместе со звёздами.
        /// </summary>
        public void SetReward(LevelRewardResult reward, Db.ShopItemDefinition unlockedItem = null)
        {
            _reward = reward;
            _rewardKnown = true;
            _unlockedItem = unlockedItem;
        }

        /// <summary>
        /// Подгоняет высоту панели под то, будет ли подарок: без него полка под карточку осталась бы
        /// пустой дырой между наградой и кнопками. Кнопки при этом подтягиваются вверх.
        /// </summary>
        private void ApplyPanelHeight()
        {
            if (panel == null || _panelHomeHeight <= 0f)
                return;

            var showsUnlock = _unlockedItem != null;
            var height = showsUnlock ? _panelHomeHeight : _panelHomeHeight - unlockBlockHeight;
            var shift = showsUnlock ? 0f : unlockBlockHeight;

            panel.sizeDelta = new Vector2(panel.sizeDelta.x, height);

            // Цели выезда кнопок пересчитываются здесь же — иначе они приехали бы на старые места,
            // под несуществующую карточку.
            _nextTarget = _nextHome + new Vector2(0f, shift);
            _menuTarget = _menuHome + new Vector2(0f, shift);

            if (nextButton != null) nextButton.anchoredPosition = _nextTarget;
            if (menuButton != null) menuButton.anchoredPosition = _menuTarget;
        }

        /// <summary>
        /// Карточка подарка: показывается только когда предмет действительно выдан на этом забеге.
        /// Играется после гемов — так два события не спорят за внимание.
        /// </summary>
        private IEnumerator UnlockRoutine()
        {
            if (unlockCard == null)
                yield break;

            unlockCard.gameObject.SetActive(_unlockedItem != null);
            if (_unlockedItem == null)
                yield break;

            if (unlockIcon != null)
                unlockIcon.sprite = _unlockedItem.Icon;

            if (unlockNameLabel != null)
                unlockNameLabel.text = global::Game.Localization.GameLocalization.ShopItemName(_unlockedItem);

            unlockCard.localScale = Vector3.zero;
            yield return WaitUnscaled(unlockDelay);

            // Заметный overshoot: подарок — самое ценное событие на экране.
            var t = 0f;
            while (t < unlockPop)
            {
                t += Time.unscaledDeltaTime;
                var s = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(Mathf.Clamp01(t / unlockPop), 2.4f));
                unlockCard.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            unlockCard.localScale = Vector3.one;

            SpawnSparkleBurst();
            StartCoroutine(LoopUnlockGlow());
        }

        private IEnumerator LoopUnlockGlow()
        {
            if (unlockGlow == null)
                yield break;

            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                var s = 1f + Mathf.Sin(t * 2.2f) * 0.08f;
                unlockGlow.localScale = new Vector3(s, s, 1f);
                unlockGlow.localRotation = Quaternion.Euler(0f, 0f, t * 14f);
                yield return null;
            }
        }

        /// <summary>
        /// Гемы, затем подарок. Через одну корутину, потому что у наград общий порядок:
        /// пропуск гемов (их может не быть на повторе) не должен съедать показ подарка.
        /// </summary>
        private IEnumerator RewardAndUnlockRoutine()
        {
            yield return RewardRoutine();
            yield return UnlockRoutine();
        }

        private IEnumerator RewardRoutine()
        {
            if (rewardRow == null)
                yield break;

            // Без награды строку не показываем совсем: пустой «+0» на экране победы выглядит
            // как ошибка, а не как результат.
            var total = _rewardKnown ? _reward.Total : 0;
            rewardRow.gameObject.SetActive(total > 0);
            if (total <= 0)
                yield break;

            if (rewardLabel != null)
                rewardLabel.text = "+0";

            rewardRow.localScale = Vector3.zero;
            yield return PopRoutine(rewardRow, 0.34f);

            // Гемы вылетают из тех звёзд, что реально заработаны.
            var earnedStars = Mathf.Clamp(_reward.Stars, 0, stars != null ? stars.Length : 0);
            var flights = Mathf.Max(1, earnedStars * gemsPerStar);
            var landed = 0;

            for (var i = 0; i < flights; i++)
            {
                var starIndex = earnedStars > 0 ? i % earnedStars : 0;
                var from = stars != null && starIndex < stars.Length && stars[starIndex] != null
                    ? stars[starIndex].position
                    : rewardRow.position;

                StartCoroutine(GemFlightRoutine(from, () => landed++));
                yield return WaitUnscaled(gemFlightStagger);
            }

            // Счётчик догоняет прилетающие гемы, а не стартует после них: так число растёт
            // одновременно с потоком, и экран не «замирает» в ожидании.
            var elapsed = 0f;
            var duration = counterDuration + gemFlightDuration;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var byTime = Mathf.Clamp01(elapsed / duration);
                var byLanded = flights > 0 ? (float) landed / flights : 1f;
                var shown = Mathf.RoundToInt(total * Mathf.Min(byTime, Mathf.Max(byLanded, byTime * 0.6f)));

                if (rewardLabel != null)
                    rewardLabel.text = "+" + shown;

                yield return null;
            }

            if (rewardLabel != null)
                rewardLabel.text = "+" + total;

            yield return PunchRoutine(rewardRow, 1.18f, 0.26f);
        }

        private IEnumerator GemFlightRoutine(Vector3 worldFrom, System.Action onLanded)
        {
            if (gemSprite == null || rewardRow == null)
            {
                onLanded?.Invoke();
                yield break;
            }

            var go = new GameObject("RewardGem", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(rewardRow.parent, false);
            rect.sizeDelta = new Vector2(52f, 52f);
            rect.position = worldFrom;

            var image = go.GetComponent<Image>();
            image.sprite = gemSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            _spawnedGems.Add(go);

            var start = rect.anchoredPosition;
            var end = ((RectTransform)rewardRow).anchoredPosition;
            // Дуга в сторону — прямой отрезок читается как «телепорт», а не как полёт.
            var control = (start + end) * 0.5f + new Vector2(UnityEngine.Random.Range(-160f, 160f), 160f);

            var t = 0f;
            while (t < gemFlightDuration)
            {
                t += Time.unscaledDeltaTime;
                var k = EaseInCubic(Mathf.Clamp01(t / gemFlightDuration));
                rect.anchoredPosition = QuadraticBezier(start, control, end, k);
                rect.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, k);
                rect.localRotation = Quaternion.Euler(0f, 0f, k * 220f);
                yield return null;
            }

            _spawnedGems.Remove(go);
            Destroy(go);
            onLanded?.Invoke();
        }

        private IEnumerator PopRoutine(RectTransform target, float duration)
        {
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var s = Mathf.LerpUnclamped(0f, 1f, EaseOutBack(Mathf.Clamp01(t / duration)));
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private IEnumerator PunchRoutine(RectTransform target, float peak, float duration)
        {
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

        private static Vector2 QuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            var u = 1f - t;
            return u * u * a + 2f * u * t * b + t * t * c;
        }

        private static float EaseInCubic(float k) => k * k * k;

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
