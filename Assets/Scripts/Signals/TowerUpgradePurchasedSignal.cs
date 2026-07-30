using Enums;

namespace Signals
{
    public sealed class TowerUpgradePurchasedSignal
    {
        public EUpgradeType upgradeType;
        public int level;
        public int paidCost;
    }
}
