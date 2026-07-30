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
            base.ReduceHealth(GetEffectiveDamage(amount));
        }

        public override int GetEffectiveDamage(int amount)
        {
            var reducedDamage = definition != null
                ? Mathf.RoundToInt(amount * (1f - definition.DamageReduction))
                : amount;

            var enemyView = _entityView as Views.Impl.EnemyView;
            var tutorialMultiplier = enemyView != null
                ? Mathf.Max(1f, enemyView.tutorialDamageTakenMultiplier)
                : 1f;
            return Mathf.Max(0, Mathf.RoundToInt(reducedDamage * tutorialMultiplier));
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
