using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace UI.Views.Upgradable
{
    public class UpgradeViewsHandler : MonoBehaviour
    {
        [SerializeField] private List<UpgradeView> upgradeViews;

        public UpgradeView GetViewByType(EUpgradeType type)
        {
            foreach (var upgradeView in upgradeViews)
            {
                if (upgradeView.UpgradeType == type)
                {
                    return upgradeView;
                }
            }

            return null;
        }
    }
}