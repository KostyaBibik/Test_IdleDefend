using System;
using Signals;
using Zenject;

namespace Systems.Actions
{
    public class LoseActionSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private bool _lose;
        
        public LoseActionSystem(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void OnLoseGame(GameLoseSignal loseSignal)
        {
            if(_lose)
                return;

            _lose = true;
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<GameLoseSignal>(OnLoseGame);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<GameLoseSignal>(OnLoseGame);
        }
    }
}