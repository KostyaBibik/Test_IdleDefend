using Db;
using Signals;
using UnityEngine;
using Zenject;

namespace Components.Enemy
{
    public class EnemyHealthComponent : HealthComponent
    {
        public SignalBus signalBus;
        public EnemyDefinition definition;

        public override void ReduceHealth(int amount)
        {
            var reducedAmount = definition != null
                ? Mathf.RoundToInt(amount * (1f - definition.DamageReduction))
                : amount;

            base.ReduceHealth(reducedAmount);
        }

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
