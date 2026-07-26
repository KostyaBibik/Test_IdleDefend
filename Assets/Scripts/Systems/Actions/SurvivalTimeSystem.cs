using System;
using Services;
using Signals;
using UnityEngine;
using Zenject;

namespace Systems.Actions
{
    /// <summary>
    /// Считает длительность забега и на проигрыше фиксирует рекорд в облачный сейв.
    /// </summary>
    public class SurvivalTimeSystem : IInitializable, ITickable, IDisposable
    {
        private readonly SignalBus _signalBus;

        private float _elapsed;
        private bool _finished;

        public SurvivalTimeSystem(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _elapsed = 0f;
            _finished = false;
            SurvivalRecord.LastRunSeconds = 0f;
            _signalBus.Subscribe<GameLoseSignal>(OnLoseGame);
        }

        public void Tick()
        {
            if (_finished)
                return;

            _elapsed += Time.deltaTime;
            SurvivalRecord.LastRunSeconds = _elapsed;
        }

        private void OnLoseGame(GameLoseSignal signal)
        {
            if (_finished)
                return;

            _finished = true;
            SurvivalRecord.SubmitRun(_elapsed);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<GameLoseSignal>(OnLoseGame);
        }
    }
}
