namespace Services
{
    public readonly struct TowerExperienceSnapshot
    {
        public TowerExperienceSnapshot(
            int currentLevel,
            int currentExperience,
            int experienceToNextLevel,
            float progress01,
            bool isLevelUpPending,
            bool isMaxLevel,
            int maxConfiguredLevel)
        {
            CurrentLevel = currentLevel;
            CurrentExperience = currentExperience;
            ExperienceToNextLevel = experienceToNextLevel;
            Progress01 = progress01;
            IsLevelUpPending = isLevelUpPending;
            IsMaxLevel = isMaxLevel;
            MaxConfiguredLevel = maxConfiguredLevel;
        }

        public int CurrentLevel { get; }
        public int CurrentExperience { get; }
        public int ExperienceToNextLevel { get; }
        public float Progress01 { get; }
        public bool IsLevelUpPending { get; }
        public bool IsMaxLevel { get; }
        public int MaxConfiguredLevel { get; }
    }
}
