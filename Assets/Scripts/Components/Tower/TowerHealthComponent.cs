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
            _lifeCounter--;
            
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