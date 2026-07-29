using System.Collections.Generic;
using System.Linq;
using Db;
using Enums;
using Signals;
using Zenject;

namespace Services
{
    public class TowerBuffSelectionService
    {
        private readonly TowerBuffRuntimeService _runtime;
        private readonly TowerExperienceService _experience;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly SignalBus _signalBus;
        private IReadOnlyList<TowerBuffDefinition> _pendingChoices = new List<TowerBuffDefinition>();

        public TowerBuffSelectionService(
            TowerBuffRuntimeService runtime,
            TowerExperienceService experience,
            IGameTimeProvider gameTimeProvider,
            SignalBus signalBus
        )
        {
            _runtime = runtime;
            _experience = experience;
            _gameTimeProvider = gameTimeProvider;
            _signalBus = signalBus;
        }

        public bool HasPendingSelection { get; private set; }
        public int PendingLevel { get; private set; }
        public IReadOnlyList<TowerBuffDefinition> PendingChoices => _pendingChoices;

        public TowerBuffSelectionSnapshot GetSnapshot()
        {
            return new TowerBuffSelectionSnapshot(HasPendingSelection, PendingLevel, _pendingChoices);
        }

        public bool TryGetPendingSelection(out int level, out IReadOnlyList<TowerBuffDefinition> choices)
        {
            level = PendingLevel;
            choices = _pendingChoices;
            return HasPendingSelection;
        }

        public void BeginSelection(int level, IReadOnlyList<TowerBuffDefinition> choices)
        {
            if (HasPendingSelection)
                return;

            PendingLevel = level;
            _pendingChoices = choices ?? new List<TowerBuffDefinition>();
            HasPendingSelection = _pendingChoices.Count > 0;

            if (!HasPendingSelection)
            {
                _experience.CompletePendingLevelUp();
                return;
            }

            _gameTimeProvider.Pause();
            _signalBus.Fire(new TowerLevelUpSignal
            {
                level = PendingLevel,
                choices = _pendingChoices
            });
        }

        public bool SelectBuffById(string buffId)
        {
            if (string.IsNullOrEmpty(buffId))
                return false;

            for (var i = 0; i < _pendingChoices.Count; i++)
            {
                var buff = _pendingChoices[i];
                if (buff != null && buff.Id == buffId)
                    return SelectBuff(buff);
            }

            return false;
        }

        public bool SelectBuff(TowerBuffDefinition buff)
        {
            if (!HasPendingSelection || buff == null || !_pendingChoices.Contains(buff))
                return false;

            var selectedLevel = PendingLevel;
            ApplySelectedBuff(buff);

            HasPendingSelection = false;
            PendingLevel = 0;
            _pendingChoices = new List<TowerBuffDefinition>();

            _signalBus.Fire(new TowerBuffSelectedSignal
            {
                level = selectedLevel,
                buff = buff
            });

            _experience.CompletePendingLevelUp();
            if (!HasPendingSelection)
                _gameTimeProvider.Resume();

            return true;
        }

        private void ApplySelectedBuff(TowerBuffDefinition buff)
        {
            switch (buff.EffectType)
            {
                case ETowerBuffEffectType.HealTower:
                    _runtime.ApplyBuff(buff);
                    _signalBus.Fire(new TowerAddHealthSignal
                    {
                        additiveCount = UnityEngine.Mathf.Max(1, UnityEngine.Mathf.RoundToInt(buff.Value))
                    });
                    break;
                default:
                    _runtime.ApplyBuff(buff);
                    break;
            }
        }
    }
}
