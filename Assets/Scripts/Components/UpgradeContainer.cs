using System;
using Enums;

namespace Components
{
    [Serializable]
    public struct UpgradeContainer
    {
        public EUpgradeType upgradeType;
        public float upgradeValue;
        public int startCost;
        public int costUpgrade;
    }
}