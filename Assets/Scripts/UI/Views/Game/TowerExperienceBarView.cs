using Game.Localization;
using Services;
using Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Game
{
    /// <summary>
    /// Полоса опыта башни и текущий уровень в HUD. Обновляется по TowerExperienceChangedSignal.
    /// </summary>
    public class TowerExperienceBarView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private GameObject maxLevelLabel;

        private SignalBus _signalBus;

        [Inject]
        public void Construct(SignalBus signalBus, TowerLevelUpUiService uiService)
        {
            _signalBus = signalBus;
            _signalBus.Subscribe<TowerExperienceChangedSignal>(OnExperienceChanged);

            // На случай, если бар появился в сцене уже после первого RaiseChanged (сброс/повторный вход) -
            // подтягиваем актуальный снапшот сразу, не дожидаясь следующего сигнала.
            var snapshot = uiService.GetExperience();
            Apply(snapshot.CurrentLevel, snapshot.Progress01, snapshot.IsMaxLevel);
        }

        private void OnExperienceChanged(TowerExperienceChangedSignal signal)
        {
            Apply(signal.currentLevel, signal.progress01, signal.isMaxLevel);
        }

        private void Apply(int level, float progress01, bool isMaxLevel)
        {
            if (levelText != null)
                levelText.text = GameLocalization.Format(LocalizationKey.tower_level_format, "Lv.{0}", level);

            if (fillImage != null)
                fillImage.fillAmount = isMaxLevel ? 1f : Mathf.Clamp01(progress01);

            if (maxLevelLabel != null)
                maxLevelLabel.SetActive(isMaxLevel);
        }

        private void OnDestroy()
        {
            _signalBus.Unsubscribe<TowerExperienceChangedSignal>(OnExperienceChanged);
        }
    }
}
