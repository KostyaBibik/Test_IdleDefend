using Signals;
using Zenject;

namespace Systems.Initializable
{
    public class GameInitializeSystem : IInitializable
    {
        private readonly SignalBus _signalBus;

        public GameInitializeSystem(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Fire(new InitializeTowerSignal());
        }
    }
}