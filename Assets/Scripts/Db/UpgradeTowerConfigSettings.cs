using System;
using System.Collections.Generic;
using Components;
using Enums;
using UnityEngine;

namespace Db
{
    [CreateAssetMenu(menuName = "Settings/" + nameof(UpgradeTowerConfigSettings),
        fileName = nameof(UpgradeTowerConfigSettings))]
    public class UpgradeTowerConfigSettings : ScriptableObject
    {
        [SerializeField] private List<UpgradeContainer> containers;
        
        
        public UpgradeContainer GetContainer(EUpgradeType upgradeType)
        {
            foreach (var container in containers)
            {
                if (container.upgradeType == upgradeType)
                    return container;
            }

            throw new Exception($"[UpgradeTowerConfigSettings] Can't find UpgradeContainer with type: {upgradeType}");
        }
        
        /*[Serializable]
        public struct UpgradeContainer
        {
            public EUpgradeType upgradeType;
            public float upgradeValue;
            public int startCost;
            public int costUpgrade;
        }*/
    }
}