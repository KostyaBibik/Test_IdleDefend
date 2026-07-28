using System.Collections.Generic;
using Db;

namespace Services
{
    public class TowerLevelUpUiService
    {
        private readonly TowerExperienceService _experienceService;
        private readonly TowerBuffSelectionService _selectionService;

        public TowerLevelUpUiService(
            TowerExperienceService experienceService,
            TowerBuffSelectionService selectionService)
        {
            _experienceService = experienceService;
            _selectionService = selectionService;
        }

        public TowerExperienceSnapshot GetExperience()
        {
            return _experienceService.GetSnapshot();
        }

        public TowerBuffSelectionSnapshot GetSelection()
        {
            return _selectionService.GetSnapshot();
        }

        public bool TryGetPendingSelection(out int level, out IReadOnlyList<TowerBuffDefinition> choices)
        {
            return _selectionService.TryGetPendingSelection(out level, out choices);
        }

        public bool SelectBuff(TowerBuffDefinition buff)
        {
            return _selectionService.SelectBuff(buff);
        }

        public bool SelectBuffById(string buffId)
        {
            return _selectionService.SelectBuffById(buffId);
        }
    }
}
