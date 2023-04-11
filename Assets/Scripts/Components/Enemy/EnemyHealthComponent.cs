using Signals;
using Zenject;

namespace Components.Enemy
{
    public class EnemyHealthComponent : HealthComponent
    {
        public SignalBus signalBus;

        protected override void Die()
        {
            if(_entityView.isDestroyed)
                return;

            signalBus?.Fire(new DestroyEntitySignal
            {
                view = _entityView,
                hashReward = _hasReward
            });
            
            base.Die();
        }
    }
}