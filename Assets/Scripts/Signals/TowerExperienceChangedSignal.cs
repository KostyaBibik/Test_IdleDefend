namespace Signals
{
    public class TowerExperienceChangedSignal
    {
        public int currentLevel;
        public int currentExperience;
        public int experienceToNextLevel;
        public float progress01;
        public bool isLevelUpPending;
        public bool isMaxLevel;
        public int maxConfiguredLevel;
    }
}
