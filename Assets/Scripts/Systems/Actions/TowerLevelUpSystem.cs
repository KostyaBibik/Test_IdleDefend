using System;
using Services;
using Zenject;
using Db;
using Tutorial;

namespace Systems.Actions
{
    public class TowerLevelUpSystem : IInitializable, IDisposable
    {
        private readonly TowerExperienceService _experience;
        private readonly TowerBuffRollService _rollService;
        private readonly TowerBuffSelectionService _selectionService;
        private readonly TutorialRuntimeState _tutorialRuntime;
        private readonly TutorialScenarioConfig _tutorialConfig;

        public TowerLevelUpSystem(
            TowerExperienceService experience,
            TowerBuffRollService rollService,
            TowerBuffSelectionService selectionService,
            TutorialRuntimeState tutorialRuntime,
            TutorialScenarioConfig tutorialConfig
        )
        {
            _experience = experience;
            _rollService = rollService;
            _selectionService = selectionService;
            _tutorialRuntime = tutorialRuntime;
            _tutorialConfig = tutorialConfig;
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
            if (_tutorialRuntime.IsRunning && _tutorialConfig.HasValidBuffChoices())
            {
                _tutorialRuntime.SetPhase(ETutorialPhase.BuffSelection);
                _selectionService.BeginSelection(level, _tutorialConfig.FirstBuffChoices);
                return;
            }

            _selectionService.BeginSelection(level, _rollService.RollChoices());
        }
    }
}
