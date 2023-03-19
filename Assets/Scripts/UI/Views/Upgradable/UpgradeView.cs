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
        
        public Button UpgradeBtn => upgradeBtn;
        public EUpgradeType UpgradeType => upgradeType;
        
        public void SetCost(int newCost)
        {
            costTxt.text = newCost.ToString();
        }
    }
}