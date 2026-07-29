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

        public Button UpgradeBtn => upgradeBtn;
        public EUpgradeType UpgradeType => upgradeType;

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
            RefreshStaticLabels();
            costTxt.text = newCost.ToString();
        }

        public void SetLevel(int current, int max, bool animate = true)
        {
            RefreshStaticLabels();
            progressBar?.SetCycledProgress(current, max, animate);
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
        }
    }
}
