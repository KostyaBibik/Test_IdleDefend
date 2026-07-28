using System.Collections.Generic;
using Db;

namespace Signals
{
    public class TowerLevelUpSignal
    {
        public int level;
        public IReadOnlyList<TowerBuffDefinition> choices;
    }
}
