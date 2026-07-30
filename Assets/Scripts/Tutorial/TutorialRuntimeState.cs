using System;
using Signals;
using Zenject;

namespace Tutorial
{
    /// <summary>
    /// Runtime gate shared by the tutorial director and the regular level systems.
    /// It is inert until Begin is called, so adding it does not change normal gameplay.
    /// </summary>
    public sealed class TutorialRuntimeState
    {
        private readonly SignalBus _signalBus;

        public TutorialRuntimeState(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public ETutorialPhase Phase { get; private set; } = ETutorialPhase.None;
        public bool IsRunning { get; private set; }
        public bool GameplayReleased { get; private set; }
        public bool BlocksStandardGameplay => IsRunning && !GameplayReleased;

        public void Begin()
        {
            if (IsRunning)
                return;

            IsRunning = true;
            GameplayReleased = false;
            SetPhase(ETutorialPhase.IntroCombat);
        }

        public void SetPhase(ETutorialPhase phase)
        {
            if (!IsRunning)
                throw new InvalidOperationException("Tutorial must be started before changing its phase.");

            if (Phase == phase)
                return;

            var previous = Phase;
            Phase = phase;
            _signalBus.Fire(new TutorialPhaseChangedSignal
            {
                previous = previous,
                current = phase
            });
        }

        public void ReleaseGameplay()
        {
            if (!IsRunning)
                return;

            GameplayReleased = true;
            SetPhase(ETutorialPhase.FreePlay);
        }

        public void Complete()
        {
            if (!IsRunning)
                return;

            GameplayReleased = true;
            SetPhase(ETutorialPhase.Completed);
            IsRunning = false;
        }

        public void Cancel()
        {
            if (!IsRunning)
                return;

            var previous = Phase;
            GameplayReleased = true;
            IsRunning = false;
            Phase = ETutorialPhase.None;
            _signalBus.Fire(new TutorialPhaseChangedSignal
            {
                previous = previous,
                current = Phase
            });
        }
    }
}
