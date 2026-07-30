using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Buffs
{
    /// <summary>
    /// Анимация окна выбора бафа: вступление (заголовок, уровень, «раздача» карт),
    /// постоянные петли (дыхание карт, пульс легендарного свечения) и короткий финал
    /// с подтверждением выбранной карты.
    ///
    /// Позиции карт задаёт HorizontalLayoutGroup на CardsRow, поэтому карты анимируются
    /// только масштабом, поворотом и альфой через CanvasGroup — эти свойства раскладка
    /// не перезаписывает, в отличие от anchoredPosition.
    ///
    /// Всё время — unscaledDeltaTime: на время выбора TowerBuffSelectionService ставит игру
    /// на паузу (timeScale = 0), и на scaled-времени анимация просто не шла бы.
    ///
    /// Финал намеренно очень короткий: SelectBuff вызывает Resume() сразу после сигнала,
    /// то есть бой продолжается уже во время финала — затягивать его нельзя.
    /// </summary>
    public class TowerLevelUpPopupAnimator : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Затемняющий фон — Image на корне попапа.")]
        [SerializeField] private Image scrim;
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform levelText;
        [SerializeField] private RectTransform subtitle;
        [Tooltip("Карточки в том же порядке, что в TowerLevelUpPopupView.cardViews.")]
        [SerializeField] private RectTransform[] cards;

        [Header("Тайминги вступления")]
        [SerializeField, Min(0f)] private float scrimFade = 0.2f;
        [SerializeField, Min(0f)] private float titlePop = 0.34f;
        [SerializeField, Min(0f)] private float levelDelay = 0.1f;
        [SerializeField, Min(0f)] private float cardsDelay = 0.24f;
        [SerializeField, Min(0f)] private float cardStagger = 0.09f;
        [SerializeField, Min(0f)] private float cardPop = 0.36f;
        [Tooltip("Начальный наклон карт при «раздаче», градусы. Крайние наклоняются в разные стороны.")]
        [SerializeField] private float cardTilt = 10f;

        [Header("Финал выбора")]
        [Tooltip("Держать коротким: бой возобновляется сразу после выбора.")]
        [SerializeField, Min(0f)] private float outroDuration = 0.28f;
        [SerializeField] private float chosenScale = 1.12f;
        [SerializeField] private float discardedScale = 0.86f;

        private float _scrimAlpha = 1f;
        private int _chosenIndex = -1;
        private bool _homeCaptured;

        // Хендлы держим отдельно и никогда не глушим всё разом изнутри корутины:
        // StopAllCoroutines() из OutroRoutine убивал бы сам финал, и окно не закрывалось.
        private Coroutine _intro;
        private Coroutine _breath;
        private Coroutine _outro;

        public void NoteChosen(int cardIndex) => _chosenIndex = cardIndex;

        /// <summary>
        /// Прерывает финал, если окно успели открыть заново (несколько уровней подряд), и
        /// возвращает карты в исходный вид — иначе новое окно показалось бы с ужатыми
        /// и полупрозрачными картами, оставшимися от недоигранного финала.
        /// </summary>
        public void CancelOutro()
        {
            Stop(ref _outro);
            ResetToHome();
        }

        /// <summary>
        /// Короткое подтверждение выбора, затем onDone. Если анимировать нечего или объект
        /// уже выключен — onDone вызывается сразу, поведение как без аниматора.
        /// </summary>
        public void PlayOutro(Action onDone)
        {
            if (!gameObject.activeInHierarchy || outroDuration <= 0f)
            {
                onDone?.Invoke();
                return;
            }

            Stop(ref _outro);
            _outro = StartCoroutine(OutroRoutine(onDone));
        }

        private void Awake() => CaptureHome();

        private void CaptureHome()
        {
            if (_homeCaptured)
                return;

            if (scrim != null)
                _scrimAlpha = scrim.color.a;

            _homeCaptured = true;
        }

        /// <summary>
        /// Запускается из TowerLevelUpPopupView.Open, а НЕ из OnEnable. Уровни приходят очередью:
        /// второй подряд левел-ап вызывает Open на уже активном окне, OnEnable при этом не придёт,
        /// и вступление бы не проигралось, а карты остались бы в состоянии прерванного финала.
        /// </summary>
        public void PlayIntro()
        {
            if (!gameObject.activeInHierarchy)
                return;

            CaptureHome();
            StopAll();
            _chosenIndex = -1;
            ResetToHome();

            _intro = StartCoroutine(Intro());
            _breath = StartCoroutine(LoopCardsBreath());
        }

        private void OnDisable()
        {
            StopAll();
            ResetToHome();
        }

        private void StopAll()
        {
            Stop(ref _intro);
            Stop(ref _breath);
            Stop(ref _outro);
        }

        private void Stop(ref Coroutine routine)
        {
            if (routine == null)
                return;

            StopCoroutine(routine);
            routine = null;
        }

        /// <summary>
        /// Возврат в исходное состояние: окно переиспользуется, и без сброса следующий показ
        /// начался бы с того, на чём кончился прошлый финал (уменьшенные и прозрачные карты).
        /// </summary>
        private void ResetToHome()
        {
            // Без снятых опорных значений сброс не имеет смысла и опасен: он записал бы в
            // альфу скрима дефолтную единицу, затерев настоящую полупрозрачность из сцены.
            // Порядок Awake на одном GameObject не определён, а TowerLevelUpPopupView.Awake
            // гасит объект — так что наш Awake может не успеть выполниться до первого OnDisable.
            if (!_homeCaptured)
                return;

            if (scrim != null) SetAlpha(scrim, _scrimAlpha);
            if (title != null) title.localScale = Vector3.one;
            if (levelText != null) { levelText.localScale = Vector3.one; levelText.localRotation = Quaternion.identity; }
            if (subtitle != null) subtitle.localScale = Vector3.one;

            if (cards == null) return;
            foreach (var card in cards)
            {
                if (card == null) continue;
                card.localScale = Vector3.one;
                card.localRotation = Quaternion.identity;
                var group = GetGroup(card);
                if (group != null) group.alpha = 1f;
            }
        }

        private IEnumerator Intro()
        {
            if (scrim != null) SetAlpha(scrim, 0f);
            if (title != null) title.localScale = Vector3.one * 0.5f;
            if (levelText != null) levelText.localScale = Vector3.zero;
            if (subtitle != null) subtitle.localScale = Vector3.zero;

            // карты прячем до своей очереди
            if (cards != null)
                for (var i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null) continue;
                    cards[i].localScale = Vector3.zero;
                    var g = GetGroup(cards[i]);
                    if (g != null) g.alpha = 0f;
                }

            // Кадр ожидания: к следующему кадру TowerLevelUpPopupView уже успел разложить
            // варианты по картам и погасить лишние (Hide), поэтому ниже видно реальный состав.
            yield return null;

            StartCoroutine(FadeScrim());
            StartCoroutine(PopIn(title, 0.5f, titlePop, 1.9f));

            yield return WaitUnscaled(levelDelay);
            StartCoroutine(PopInSpin(levelText, levelPop: 0.34f));
            StartCoroutine(PopIn(subtitle, 0f, 0.3f, 1.5f));

            yield return WaitUnscaled(Mathf.Max(0f, cardsDelay - levelDelay));

            if (cards != null)
                for (var i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null || !cards[i].gameObject.activeSelf)
                        continue;

                    // крайние карты «падают» с наклоном в разные стороны, центральная ровно
                    var tilt = cards.Length > 1 ? Mathf.Lerp(-cardTilt, cardTilt, i / (float)(cards.Length - 1)) : 0f;
                    StartCoroutine(DealCard(cards[i], tilt));
                    yield return WaitUnscaled(cardStagger);
                }
        }

        private IEnumerator FadeScrim()
        {
            if (scrim == null) yield break;
            var t = 0f;
            while (t < scrimFade)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(scrim, Mathf.Lerp(0f, _scrimAlpha, Mathf.Clamp01(t / scrimFade)));
                yield return null;
            }
            SetAlpha(scrim, _scrimAlpha);
        }

        private IEnumerator PopIn(RectTransform target, float from, float duration, float overshoot)
        {
            if (target == null) yield break;
            var t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, EaseOutBack(k, overshoot));
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        private IEnumerator PopInSpin(RectTransform target, float levelPop)
        {
            if (target == null) yield break;
            var t = 0f;
            while (t < levelPop)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / levelPop);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, EaseOutBack(k, 2.4f));
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(-140f, 0f, EaseOutCubic(k)));
                yield return null;
            }
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.identity;
        }

        private IEnumerator DealCard(RectTransform card, float tiltFrom)
        {
            var group = GetGroup(card);
            var t = 0f;
            while (t < cardPop)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / cardPop);
                var e = EaseOutBack(k, 2.0f);
                card.localScale = Vector3.one * Mathf.LerpUnclamped(0.55f, 1f, e);
                card.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(tiltFrom, 0f, EaseOutCubic(k)));
                if (group != null) group.alpha = Mathf.Clamp01(k / 0.4f);
                yield return null;
            }
            card.localScale = Vector3.one;
            card.localRotation = Quaternion.identity;
            if (group != null) group.alpha = 1f;
        }

        private IEnumerator LoopCardsBreath()
        {
            if (cards == null) yield break;
            yield return WaitUnscaled(cardsDelay + cardPop + cardStagger * cards.Length);

            var t = 0f;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                for (var i = 0; i < cards.Length; i++)
                {
                    if (cards[i] == null || !cards[i].gameObject.activeSelf) continue;
                    // Разные фазы, чтобы карты дышали не в унисон. Амплитуда намеренно
                    // маленькая — это фон, а не привлечение внимания к конкретной карте.
                    var s = 1f + Mathf.Sin(t * 1.9f + i * 2.1f) * 0.012f;
                    cards[i].localScale = new Vector3(s, s, 1f);
                }
                yield return null;
            }
        }

        private IEnumerator OutroRoutine(Action onDone)
        {
            // Глушим только вступление и дыхание — но не себя.
            Stop(ref _intro);
            Stop(ref _breath);

            var t = 0f;
            while (t < outroDuration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / outroDuration);

                if (cards != null)
                    for (var i = 0; i < cards.Length; i++)
                    {
                        if (cards[i] == null || !cards[i].gameObject.activeSelf) continue;
                        var group = GetGroup(cards[i]);

                        if (i == _chosenIndex)
                        {
                            // выбранная — подскок и остаётся видимой
                            cards[i].localScale = Vector3.one * Mathf.LerpUnclamped(1f, chosenScale, EaseOutBack(k, 2.6f));
                        }
                        else
                        {
                            // остальные ужимаются и гаснут
                            cards[i].localScale = Vector3.one * Mathf.Lerp(1f, discardedScale, EaseOutCubic(k));
                            if (group != null) group.alpha = 1f - EaseOutCubic(k);
                        }
                    }

                if (scrim != null)
                    SetAlpha(scrim, Mathf.Lerp(_scrimAlpha, 0f, EaseOutCubic(k)));

                yield return null;
            }

            _outro = null;
            onDone?.Invoke();
        }

        private static CanvasGroup GetGroup(Component target)
        {
            if (target == null) return null;
            var g = target.GetComponent<CanvasGroup>();
            return g != null ? g : target.gameObject.AddComponent<CanvasGroup>();
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
