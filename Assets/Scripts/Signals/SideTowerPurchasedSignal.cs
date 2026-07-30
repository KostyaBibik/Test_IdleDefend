using Db;
using UI.Views;

namespace Signals
{
    public sealed class SideTowerPurchasedSignal
    {
        public SideTowerDefinition definition;
        public int paidCost;
        public SideTowerSlotMarkerView marker;
    }
}
