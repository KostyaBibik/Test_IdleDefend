using Signals;
using Zenject;

namespace Components.Enemy
{
    public class EnemyHealthComponent : HealthComponent
    {
        private SignalBus _signalBus;
        
        [Inject]
        public void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        protected override void Die()
        {
            _signalBus.Fire(new DestroyEntitySignal
            {
                view = _entityView
            });
            
            base.Die();
        }
    }
}