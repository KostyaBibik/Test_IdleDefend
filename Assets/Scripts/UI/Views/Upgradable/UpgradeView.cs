using Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Upgradable
{
    public class UpgradeView : MonoBehaviour
    {
        [SerializeField] private Button upgradeBtn;
        [SerializeField] private TMP_Text costTxt;
        [SerializeField] private EUpgradeType upgradeType;
        [SerializeField] private UpgradeProgressBarView progressBar;

        public Button UpgradeBtn => upgradeBtn;
        public EUpgradeType UpgradeType => upgradeType;

        public void SetCost(int newCost)
        {
            costTxt.text = newCost.ToString();
        }

        public void SetLevel(int current, int max, bool animate = true)
        {
            progressBar?.SetCycledProgress(current, max, animate);
        }
    }
}
