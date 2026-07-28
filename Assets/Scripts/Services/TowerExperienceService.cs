using System;
using Db;
using Signals;
using Zenject;

namespace Services
{
    public class TowerExperienceService : IInitializable
    {
        private readonly TowerExperienceConfig _config;
        private readonly SignalBus _signalBus;
        private bool _levelUpPending;

        public TowerExperienceService(TowerExperienceConfig config, SignalBus signalBus)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public event Action<int> OnLevelUpReady;
        public event Action OnChanged;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExperience { get; private set; }
        public int ExperienceToNextLevel => _config.GetExperienceToNextLevel(CurrentLevel);
        public int MaxConfiguredLevel => _config.MaxConfiguredLevel;
        public bool IsLevelUpPending => _levelUpPending;
        public bool IsMaxLevel => !_config.HasNextLevel(CurrentLevel);
        public float Progress01 => ExperienceToNextLevel <= 0 ? 1f : (float)CurrentExperience / ExperienceToNextLevel;

        public void Initialize()
        {
            CurrentLevel = 1;
            CurrentExperience = 0;
            _levelUpPending = false;
            RaiseChanged();
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0)
                return;

            if (IsMaxLevel)
                return;

            CurrentExperience += amount;
            TryStartLevelUp();
            RaiseChanged();
        }

        public void CompletePendingLevelUp()
        {
            if (!_levelUpPending)
                return;

            _levelUpPending = false;
            TryStartLevelUp();
            RaiseChanged();
        }

        public TowerExperienceSnapshot GetSnapshot()
        {
            return new TowerExperienceSnapshot(
                CurrentLevel,
                CurrentExperience,
                ExperienceToNextLevel,
                Progress01,
                IsLevelUpPending,
                IsMaxLevel,
                MaxConfiguredLevel);
        }

        private void TryStartLevelUp()
        {
            if (_levelUpPending)
                return;

            if (IsMaxLevel)
            {
                CurrentExperience = 0;
                return;
            }

            var needed = ExperienceToNextLevel;
            if (needed <= 0 || CurrentExperience < needed)
                return;

            CurrentExperience = 0;
            CurrentLevel++;
            _levelUpPending = true;
            OnLevelUpReady?.Invoke(CurrentLevel);
        }

        private void RaiseChanged()
        {
            OnChanged?.Invoke();

            _signalBus.Fire(new TowerExperienceChangedSignal
            {
                currentLevel = CurrentLevel,
                currentExperience = CurrentExperience,
                experienceToNextLevel = ExperienceToNextLevel,
                progress01 = Progress01,
                isLevelUpPending = IsLevelUpPending,
                isMaxLevel = IsMaxLevel,
                maxConfiguredLevel = MaxConfiguredLevel
            });
        }
    }
}
