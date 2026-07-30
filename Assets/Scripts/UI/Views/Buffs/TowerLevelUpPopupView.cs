using System;
using System.Collections.Generic;
using Db;
using Game.Localization;
using Services;
using Signals;
using TMPro;
using UnityEngine;
using Zenject;

namespace UI.Views.Buffs
{
    /// <summary>
    /// Модальное окно выбора бафа при левел-апе башни (аналог экрана выбора навыка в Archero).
    /// Открывается по TowerLevelUpSignal, закрывается после TowerBuffSelectedSignal.
    /// Кнопки пропуска нет - пауза и логика выбора уже реализованы в TowerBuffSelectionService.
    /// </summary>
    public class TowerLevelUpPopupView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TowerBuffCardView[] cardViews;

        [Tooltip("Необязателен: без него окно открывается и закрывается мгновенно, как раньше.")]
        [SerializeField] private TowerLevelUpPopupAnimator animator;

        private SignalBus _signalBus;
        private TowerLevelUpUiService _uiService;

        // Уровней может прийти несколько подряд: пока играется финал закрытия, окно уже может
        // открыться заново. Счётчик позволяет отложенному закрытию понять, что оно устарело.
        private int _openGeneration;

        public TowerBuffCardView GetCardFor(TowerBuffDefinition buff)
        {
            if (buff == null || cardViews == null)
                return null;

            foreach (var card in cardViews)
            {
                if (card != null && card.Buff == buff)
                    return card;
            }

            return null;
        }

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        [Inject]
        public void Construct(SignalBus signalBus, TowerLevelUpUiService uiService)
        {
            _signalBus = signalBus;
            _uiService = uiService;

            _signalBus.Subscribe<TowerLevelUpSignal>(OnLevelUp);
            _signalBus.Subscribe<TowerBuffSelectedSignal>(OnBuffSelected);
            _signalBus.Subscribe<GameWinSignal>(OnGameWin);

            // Если сцена стартовала с уже отложенным выбором (окно не успело подписаться до
            // BeginSelection), открываем его сразу, а не ждём следующего левел-апа.
            if (_uiService.TryGetPendingSelection(out var level, out var choices))
                Open(level, choices);
        }

        private void OnLevelUp(TowerLevelUpSignal signal)
        {
            Open(signal.level, signal.choices);
        }

        private void OnBuffSelected(TowerBuffSelectedSignal signal)
        {
            Close();
        }

        private void OnGameWin(GameWinSignal signal)
        {
            _openGeneration++;
            animator?.CancelOutro();
            gameObject.SetActive(false);
        }

        private void Open(int level, IReadOnlyList<TowerBuffDefinition> choices)
        {
            RefreshStaticLabels();

            _openGeneration++;
            // Прошлый финал закрытия мог не успеть доиграть — он не должен трогать новое окно.
            animator?.CancelOutro();

            if (levelText != null)
                levelText.text = GameLocalization.Format(LocalizationKey.tower_level_format, "Lv.{0}", level);

            for (var i = 0; i < cardViews.Length; i++)
            {
                var card = cardViews[i];
                if (card == null)
                    continue;

                if (choices != null && i < choices.Count && choices[i] != null)
                {
                    var cardIndex = i;
                    card.Setup(choices[i], buff => OnCardSelected(buff, cardIndex));
                }
                else
                {
                    card.Hide();
                }
            }

            gameObject.SetActive(true);

            // Явно, а не через OnEnable: при втором левел-апе подряд окно уже активно,
            // и OnEnable не пришёл бы.
            animator?.PlayIntro();
        }

        private void RefreshStaticLabels()
        {
            titleText ??= GameLocalization.FindTextByCurrentValue(this, "LEVEL UP", "Level up", "Новый уровень");
            subtitleText ??= GameLocalization.FindTextByCurrentValue(this, "Choose a new skill!", "Choose a new skill", "Выберите новый навык");

            if (titleText != null)
                titleText.text = GameLocalization.Text(LocalizationKey.tower_level_up_title, "Level up");

            if (subtitleText != null)
                subtitleText.text = GameLocalization.Text(LocalizationKey.tower_level_up_subtitle, "Choose a new skill");
        }

        private void Close()
        {
            if (animator == null || !gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                return;
            }

            // Финал короткий, но за это время может прийти следующий левел-ап и открыть окно
            // заново — тогда гасить его нельзя, иначе игрок останется без выбора.
            var generation = _openGeneration;
            animator.PlayOutro(() =>
            {
                if (generation == _openGeneration)
                    gameObject.SetActive(false);
            });
        }

        private void OnCardSelected(TowerBuffDefinition buff, int cardIndex)
        {
            // Запоминаем выбор до применения: SelectBuffById синхронно приводит к Close().
            animator?.NoteChosen(cardIndex);
            _uiService.SelectBuffById(buff.Id);
        }

        private void OnDestroy()
        {
            _signalBus?.Unsubscribe<TowerLevelUpSignal>(OnLevelUp);
            _signalBus?.Unsubscribe<TowerBuffSelectedSignal>(OnBuffSelected);
            _signalBus?.Unsubscribe<GameWinSignal>(OnGameWin);
        }
    }
}
