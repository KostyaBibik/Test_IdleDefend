using System;
using Services;
using Zenject;

namespace Systems.Actions
{
    public class TowerLevelUpSystem : IInitializable, IDisposable
    {
        private readonly TowerExperienceService _experience;
        private readonly TowerBuffRollService _rollService;
        private readonly TowerBuffSelectionService _selectionService;

        public TowerLevelUpSystem(
            TowerExperienceService experience,
            TowerBuffRollService rollService,
            TowerBuffSelectionService selectionService
        )
        {
            _experience = experience;
            _rollService = rollService;
            _selectionService = selectionService;
        }

        public void Initialize()
        {
            _experience.OnLevelUpReady += OnLevelUpReady;
        }

        public void Dispose()
        {
            _experience.OnLevelUpReady -= OnLevelUpReady;
        }

        private void OnLevelUpReady(int level)
        {
            _selectionService.BeginSelection(level, _rollService.RollChoices());
        }
    }
}
