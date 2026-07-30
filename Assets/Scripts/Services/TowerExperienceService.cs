using System;
using System.Collections.Generic;
using Db;
using Signals;
using UnityEngine;
using Zenject;

namespace Services
{
    public class TowerExperienceService : IInitializable, ITickable, IDisposable
    {
        private readonly TowerExperienceConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Queue<int> _queuedLevelUps = new();
        private bool _levelUpPending;
        private bool _levelFinished;
        private float _drainCarry;
        private float _levelUpDelayTimer;

        public TowerExperienceService(TowerExperienceConfig config, SignalBus signalBus)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public event Action<int> OnLevelUpReady;
        public event Action OnChanged;

        public int CurrentLevel { get; private set; } = 1;
        public int CurrentExperience { get; private set; }
        public int PendingExperience { get; private set; }
        public int ExperienceToNextLevel => _config.GetExperienceToNextLevel(CurrentLevel);
        public int MaxConfiguredLevel => _config.MaxConfiguredLevel;
        public bool IsLevelUpPending => _levelUpPending;
        public int QueuedLevelUps => _queuedLevelUps.Count;
        public bool IsMaxLevel => !_config.HasNextLevel(CurrentLevel);
        public float Progress01 => ExperienceToNextLevel <= 0 ? 1f : (float)CurrentExperience / ExperienceToNextLevel;

        public void Initialize()
        {
            CurrentLevel = 1;
            CurrentExperience = 0;
            PendingExperience = 0;
            _queuedLevelUps.Clear();
            _levelUpPending = false;
            _levelFinished = false;
            _drainCarry = 0f;
            _levelUpDelayTimer = 0f;

            _signalBus.Subscribe<TowerExperienceOrbArrivedSignal>(OnOrbArrived);

            RaiseChanged();
        }

        public void AddExperience(int amount)
        {
            AddExperience(amount, Vector3.zero);
        }

        /// <summary>
        /// Враг умер, опыт "выпал" - но в PendingExperience он попадёт не отсюда. Этот метод
        /// только объявляет дроп (для TowerExperienceOrbSystem: спавнит летящие орбы), а
        /// фактическое начисление приходит позже, по TowerExperienceOrbArrivedSignal — в момент,
        /// когда опыт визуально долетел до бара, а не в момент смерти врага.
        /// </summary>
        public void AddExperience(int amount, Vector3 worldPosition)
        {
            if (_levelFinished)
                return;

            if (amount <= 0)
                return;

            if (IsMaxLevel)
                return;

            _signalBus.Fire(new TowerExperienceDroppedSignal
            {
                amount = amount,
                worldPosition = worldPosition
            });
        }

        /// <summary>
        /// Опыт долетел до бара (или системе визуала нечем было его показать - см. её fallback) -
        /// только теперь он реально идёт в счёт. Если игрок за время полёта уже достиг макс.
        /// уровня, сумма сгорает - как и раньше сгорал бы весь AddExperience в этом случае.
        /// </summary>
        private void OnOrbArrived(TowerExperienceOrbArrivedSignal signal)
        {
            if (_levelFinished)
                return;

            if (signal.amount <= 0)
                return;

            if (IsMaxLevel)
                return;

            PendingExperience += signal.amount;
            RaiseChanged();
        }

        public void Tick()
        {
            if (_levelFinished)
                return;

            if (_levelUpPending)
                return;

            var changed = DrainPendingExperience(Time.unscaledDeltaTime);
            changed |= TickLevelUpDelay(Time.unscaledDeltaTime);

            if (changed)
                RaiseChanged();
        }

        public void CompletePendingLevelUp()
        {
            if (_levelFinished)
                return;

            if (!_levelUpPending)
                return;

            _levelUpPending = false;
            TryStartQueuedLevelUp(ignoreDelay: true);
            RaiseChanged();
        }

        public void FinishLevel()
        {
            if (_levelFinished)
                return;

            _levelFinished = true;
            PendingExperience = 0;
            _queuedLevelUps.Clear();
            _levelUpPending = false;
            _drainCarry = 0f;
            _levelUpDelayTimer = 0f;
            RaiseChanged();
        }

        public TowerExperienceSnapshot GetSnapshot()
        {
            return new TowerExperienceSnapshot(
                CurrentLevel,
                CurrentExperience,
                ExperienceToNextLevel,
                PendingExperience,
                QueuedLevelUps,
                Progress01,
                IsLevelUpPending,
                IsMaxLevel,
                MaxConfiguredLevel);
        }

        private bool DrainPendingExperience(float deltaTime)
        {
            if (PendingExperience <= 0 || IsMaxLevel || deltaTime <= 0f)
                return false;

            var drainSpeed = Mathf.Clamp(
                _config.ExperienceDrainBaseSpeed + PendingExperience * _config.ExperienceDrainPendingMultiplier,
                1f,
                Mathf.Max(1f, _config.ExperienceDrainMaxSpeed));

            var rawAmount = drainSpeed * deltaTime + _drainCarry;
            var amount = Mathf.FloorToInt(rawAmount);
            _drainCarry = rawAmount - amount;

            if (amount <= 0)
                return false;

            amount = Mathf.Min(amount, PendingExperience);
            PendingExperience -= amount;
            ApplyExperience(amount);
            TryStartQueuedLevelUp(ignoreDelay: false);
            return true;
        }

        private void ApplyExperience(int amount)
        {
            while (amount > 0 && !IsMaxLevel)
            {
                var needed = ExperienceToNextLevel;
                if (needed <= 0)
                    break;

                var remainingToLevelUp = needed - CurrentExperience;
                if (amount < remainingToLevelUp)
                {
                    CurrentExperience += amount;
                    return;
                }

                amount -= remainingToLevelUp;
                CurrentExperience = 0;
                CurrentLevel++;
                _queuedLevelUps.Enqueue(CurrentLevel);
                _levelUpDelayTimer = Mathf.Max(_levelUpDelayTimer, _config.LevelUpPopupDelay);
            }

            if (IsMaxLevel)
            {
                CurrentExperience = 0;
                PendingExperience = 0;
                _drainCarry = 0f;
            }
        }

        private bool TickLevelUpDelay(float deltaTime)
        {
            if (_queuedLevelUps.Count <= 0)
                return false;

            if (_levelUpDelayTimer > 0f)
            {
                _levelUpDelayTimer = Mathf.Max(0f, _levelUpDelayTimer - deltaTime);
                if (_levelUpDelayTimer > 0f)
                    return false;
            }

            TryStartQueuedLevelUp(ignoreDelay: true);
            return true;
        }

        private void TryStartQueuedLevelUp(bool ignoreDelay)
        {
            if (_levelUpPending || _queuedLevelUps.Count <= 0)
                return;

            if (!ignoreDelay && _levelUpDelayTimer > 0f)
                return;

            var level = _queuedLevelUps.Dequeue();
            _levelUpDelayTimer = 0f;
            _levelUpPending = true;
            OnLevelUpReady?.Invoke(level);
        }

        private void RaiseChanged()
        {
            OnChanged?.Invoke();

            _signalBus.Fire(new TowerExperienceChangedSignal
            {
                currentLevel = CurrentLevel,
                currentExperience = CurrentExperience,
                experienceToNextLevel = ExperienceToNextLevel,
                pendingExperience = PendingExperience,
                queuedLevelUps = QueuedLevelUps,
                progress01 = Progress01,
                isLevelUpPending = IsLevelUpPending,
                isMaxLevel = IsMaxLevel,
                maxConfiguredLevel = MaxConfiguredLevel
            });
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<TowerExperienceOrbArrivedSignal>(OnOrbArrived);
        }
    }
}
