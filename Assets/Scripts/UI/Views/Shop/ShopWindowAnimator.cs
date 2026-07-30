using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Views.Shop
{
    /// <summary>
    /// Оживляет окно магазина: вступление при открытии, выкладку карточек стаггером при смене
    /// вкладки и отклик на покупку/экипировку.
    ///
    /// Карточки лежат в GridLayoutGroup, а вкладки — в HorizontalLayoutGroup: раскладка каждый
    /// кадр переписывает их позиции, поэтому анимируем только localScale и alpha.
    ///
    /// Выкладку карточек намеренно не вешаем на каждую перерисовку: ShopWindowView перерисовывает
    /// вкладку и при смене баланса, и сетка дёргалась бы после каждой покупки.
    /// </summary>
    public class ShopWindowAnimator : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private RectTransform panel;
        [SerializeField] private RectTransform topBar;
        [Tooltip("Контейнер вкладок: анимируются его активные дети.")]
        [SerializeField] private RectTransform tabBar;
        [SerializeField] private RectTransform selectedItemPanel;

        [Header("Тайминги")]
        [SerializeField, Min(0.01f)] private float panelPop = 0.34f;
        [SerializeField, Min(0f)] private float tabsDelay = 0.1f;
        [SerializeField, Min(0f)] private float tabStagger = 0.05f;
        [SerializeField, Min(0.01f)] private float tabPop = 0.26f;
        [SerializeField, Min(0f)] private float cardsDelay = 0.16f;
        [SerializeField, Min(0f)] private float cardStagger = 0.045f;
        [SerializeField, Min(0.01f)] private float cardPop = 0.3f;

        [Header("Отклик")]
        [SerializeField] private float punchScale = 1.14f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.28f;

        private readonly List<Coroutine> _running = new();

        // Карточки, которым мы трогали масштаб: их надо вернуть в единицу при закрытии окна,
        // иначе прерванная выкладка оставит их сжатыми в точку до следующей анимации.
        private readonly List<ShopItemButtonView> _touchedCards = new();

        private void OnDisable()
        {
            StopAll();
            // Окно переиспользуется: без сброса следующее открытие началось бы с недоигранных
            // масштабов, и часть карточек осталась бы сжатой в точку.
            ResetToHome();
        }

        /// <summary>
        /// Вступление окна. Вызывается из ShopWindowView, а не из OnEnable: аниматор и окно висят
        /// на одном объекте, порядок их OnEnable не определён, и StopAll() внутри вступления мог бы
        /// прибить уже запущенную выкладку карточек — те остались бы сжатыми в точку.
        /// </summary>
        public void PlayOpen()
        {
            if (!gameObject.activeInHierarchy)
                return;

            StopAll();
            Track(StartCoroutine(OpenRoutine()));
        }

        /// <summary>
        /// Выкладка карточек. Вызывается при открытии окна и смене вкладки — но не при обычной
        /// перерисовке состояния.
        /// </summary>
        public void PlayItems(IReadOnlyList<ShopItemButtonView> cards)
        {
            if (!gameObject.activeInHierarchy || cards == null)
                return;

            Track(StartCoroutine(CardsRoutine(cards, cardsDelay)));
        }

        /// <summary>Отклик на покупку или экипировку конкретной карточки.</summary>
        public void PunchCard(ShopItemButtonView card)
        {
            Punch(card != null ? card.transform as RectTransform : null);
        }

        /// <summary>Отклик на переключение вкладки.</summary>
        public void PunchTab(ShopTabButtonView tab)
        {
            Punch(tab != null ? tab.transform as RectTransform : null);
        }

        private void Punch(RectTransform target)
        {
            if (!gameObject.activeInHierarchy || target == null)
                return;

            Track(StartCoroutine(PunchRoutine(target)));
        }

        private IEnumerator OpenRoutine()
        {
            if (panel != null)
            {
                panel.localScale = new Vector3(0.9f, 0.9f, 1f);
                Track(StartCoroutine(PopRoutine(panel, 0.9f, panelPop)));
            }

            if (topBar != null)
            {
                topBar.localScale = new Vector3(1f, 0.7f, 1f);
                Track(StartCoroutine(PopRoutine(topBar, 0.7f, panelPop)));
            }

            if (selectedItemPanel != null)
            {
                selectedItemPanel.localScale = new Vector3(1f, 0.8f, 1f);
                Track(StartCoroutine(PopRoutine(selectedItemPanel, 0.8f, panelPop)));
            }

            // Гасим до задержки: иначе вкладки успели бы мигнуть в полный размер.
            var tabs = CollectTabs();
            foreach (var tab in tabs)
                tab.localScale = Vector3.zero;

            yield return WaitUnscaled(tabsDelay);

            for (var i = 0; i < tabs.Count; i++)
                Track(StartCoroutine(DelayedPop(tabs[i], tabPop, i * tabStagger)));
        }

        private List<RectTransform> CollectTabs()
        {
            var result = new List<RectTransform>();
            if (tabBar == null)
                return result;

            for (var i = 0; i < tabBar.childCount; i++)
            {
                if (tabBar.GetChild(i) is RectTransform tab && tab.gameObject.activeSelf)
                    result.Add(tab);
            }

            return result;
        }

        private IEnumerator CardsRoutine(IReadOnlyList<ShopItemButtonView> cards, float delay)
        {
            // Гасим сразу, до задержки: иначе карточки успели бы мигнуть в полный размер.
            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                card.transform.localScale = Vector3.zero;

                if (!_touchedCards.Contains(card))
                    _touchedCards.Add(card);
            }

            yield return WaitUnscaled(delay);

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                Track(StartCoroutine(DelayedPop((RectTransform)card.transform, cardPop, i * cardStagger)));
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
            if (target == null)
                yield break;

            var t = 0f;
            while (t < punchDuration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / punchDuration);
                // вверх быстро, обратно с отскоком
                var s = k < 0.3f
                    ? Mathf.Lerp(1f, punchScale, k / 0.3f)
                    : Mathf.LerpUnclamped(punchScale, 1f, EaseOutBack((k - 0.3f) / 0.7f, 1.2f));
                target.localScale = new Vector3(s, s, 1f);
                yield return null;
            }

            target.localScale = Vector3.one;
        }

        private void ResetToHome()
        {
            if (panel != null) panel.localScale = Vector3.one;
            if (topBar != null) topBar.localScale = Vector3.one;
            if (selectedItemPanel != null) selectedItemPanel.localScale = Vector3.one;

            if (tabBar != null)
                for (var i = 0; i < tabBar.childCount; i++)
                    tabBar.GetChild(i).localScale = Vector3.one;

            foreach (var card in _touchedCards)
                if (card != null)
                    card.transform.localScale = Vector3.one;

            _touchedCards.Clear();
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

        private static float EaseOutBack(float k, float overshoot = 1.7f)
        {
            var c = overshoot + 1f;
            return 1f + c * Mathf.Pow(k - 1f, 3f) + overshoot * Mathf.Pow(k - 1f, 2f);
        }
    }
}
