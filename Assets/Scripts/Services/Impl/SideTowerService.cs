using System.Collections.Generic;
using Signals;
using Views;
using Views.Impl;

namespace Services.Impl
{
    public class SideTowerService : IEntityService
    {
        public List<SideTowerView> Towers { get; } = new();

        public void AddEntityOnService(IEntityView entityView)
        {
            Towers.Add((SideTowerView) entityView);
        }

        public void RemoveEntityFromService(DestroyEntitySignal signal)
        {
            // доп-башни неуничтожимы, метод нужен только для соответствия IEntityService
        }
    }
}
