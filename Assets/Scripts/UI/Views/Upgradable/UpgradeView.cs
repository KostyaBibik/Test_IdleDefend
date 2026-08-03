using Enums;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using VYandexTools.Localization.Scripts;

namespace UI.Views.Upgradable
{
    public class UpgradeView : MonoBehaviour
    {
        [SerializeField] private Button upgradeBtn;
        [SerializeField] private TMP_Text costTxt;
        [SerializeField] private TMP_Text titleTxt;
        [SerializeField] private TMP_Text buttonLabel;
        [SerializeField] private EUpgradeType upgradeType;
        [SerializeField] private UpgradeProgressBarView progressBar;

        private int _currentCost;
        private bool _isMaxed;

        public Button UpgradeBtn => upgradeBtn;
        public EUpgradeType UpgradeType => upgradeType;

        private void Awake()
        {
            // Heal — разовое расходуемое действие, а не постоянный стат с уровнями,
            // поэтому у него нет смысла показывать прогресс-бар цикла апгрейда.
            if (upgradeType == EUpgradeType.UpHealth && progressBar != null)
                progressBar.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            RefreshStaticLabels();
        }

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        public void SetCost(int newCost)
        {
            _currentCost = newCost;
            RefreshStaticLabels();
            RenderCost();
        }

        public void SetLevel(int current, int max, bool animate = true)
        {
            RefreshStaticLabels();

            if (upgradeType != EUpgradeType.UpHealth)
                progressBar?.SetCycledProgress(current, max, animate);
        }

        /// <summary>
        /// isMaxed — стат/здоровье уже на потолке, canAfford — хватает ли монет на покупку.
        /// Разделены, чтобы UI мог по-разному показывать "уже макс" и "коплю монеты".
        /// </summary>
        public void RefreshState(bool isMaxed, bool canAfford)
        {
            _isMaxed = isMaxed;
            RenderCost();

            if (upgradeBtn != null)
                upgradeBtn.interactable = !isMaxed && canAfford;
        }

        private void RenderCost()
        {
            if (costTxt == null)
                return;

            costTxt.text = _isMaxed
                ? GameLocalization.Text(LocalizationKey.max_level, "MAX")
                : _currentCost.ToString();
        }

        private void RefreshStaticLabels()
        {
            titleTxt ??= FindTitleText();
            buttonLabel ??= upgradeBtn != null ? upgradeBtn.GetComponentInChildren<TMP_Text>(true) : null;
            DisableLocalizedText(titleTxt);
            DisableLocalizedText(buttonLabel);

            if (titleTxt != null)
                titleTxt.text = upgradeType switch
                {
                    EUpgradeType.RangeAttack => GameLocalization.Text(LocalizationKey.upgrade_range, "Range"),
                    EUpgradeType.AttackDamage => GameLocalization.Text(LocalizationKey.upgrade_damage, "Damage"),
                    EUpgradeType.AttackSpeed => GameLocalization.Text(LocalizationKey.upgrade_attack_speed, "Atk. speed"),
                    EUpgradeType.UpHealth => GameLocalization.Text(LocalizationKey.upgrade_health, "Health"),
                    _ => titleTxt.text
                };

            if (buttonLabel != null)
                buttonLabel.text = GameLocalization.Text(LocalizationKey.upgrade_buy, "Buy");
        }

        private TMP_Text FindTitleText()
        {
            var labels = GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                if (label == null || label == costTxt)
                    continue;

                if (upgradeBtn != null && label.transform.IsChildOf(upgradeBtn.transform))
                    continue;

                return label;
            }

            return null;
        }

        private static void DisableLocalizedText(TMP_Text label)
        {
            if (label == null)
                return;

            var localizedText = label.GetComponent<LocalizedTextTMP>();
            if (localizedText != null)
                localizedText.enabled = false;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            RefreshStaticLabels();
            RenderCost();
        }
    }
}
