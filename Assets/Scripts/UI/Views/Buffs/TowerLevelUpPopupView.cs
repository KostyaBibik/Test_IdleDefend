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

        private SignalBus _signalBus;
        private TowerLevelUpUiService _uiService;

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

        private void Open(int level, IReadOnlyList<TowerBuffDefinition> choices)
        {
            RefreshStaticLabels();

            if (levelText != null)
                levelText.text = GameLocalization.Format(LocalizationKey.tower_level_format, "Lv.{0}", level);

            for (var i = 0; i < cardViews.Length; i++)
            {
                var card = cardViews[i];
                if (card == null)
                    continue;

                if (choices != null && i < choices.Count && choices[i] != null)
                    card.Setup(choices[i], OnCardSelected);
                else
                    card.Hide();
            }

            gameObject.SetActive(true);
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
            gameObject.SetActive(false);
        }

        private void OnCardSelected(TowerBuffDefinition buff)
        {
            _uiService.SelectBuffById(buff.Id);
        }

        private void OnDestroy()
        {
            _signalBus.Unsubscribe<TowerLevelUpSignal>(OnLevelUp);
            _signalBus.Unsubscribe<TowerBuffSelectedSignal>(OnBuffSelected);
        }
    }
}
