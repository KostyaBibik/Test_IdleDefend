using System.Collections.Generic;
using Db;

namespace Services
{
    public readonly struct TowerBuffSelectionSnapshot
    {
        public TowerBuffSelectionSnapshot(bool hasPendingSelection, int pendingLevel, IReadOnlyList<TowerBuffDefinition> choices)
        {
            HasPendingSelection = hasPendingSelection;
            PendingLevel = pendingLevel;
            Choices = choices;
        }

        public bool HasPendingSelection { get; }
        public int PendingLevel { get; }
        public IReadOnlyList<TowerBuffDefinition> Choices { get; }
    }
}
