using Signals;
using UnityEngine;
using Zenject;

namespace Components.Tower
{
    public class TowerHealthComponent : MonoBehaviour, IHealable
    {
        private int _lifeCounter;

        private SignalBus _signalBus;
        
        public void AddHealth(int amount)
        {
            _lifeCounter += amount;
        }

        public void ReduceHealth(int amount)
        {
            // Раньше здесь всегда снимался ровно 1 счётчик, из-за чего EnemyDefinition.damageToTower
            // не влиял ни на что. Танк с damageToTower: 2 обязан стоить двух жизней.
            _lifeCounter -= Mathf.Max(1, amount);

            if (_lifeCounter <= 0)
                Die();
        }

        public int GetHealth()
        {
            return _lifeCounter;
        }

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void Die()
        {
            _signalBus.Fire<GameLoseSignal>();
        }
    }
}