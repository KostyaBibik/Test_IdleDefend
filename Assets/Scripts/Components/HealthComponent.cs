using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Components
{
    public abstract class HealthComponent : MonoBehaviour, IHealable
    {
        protected IEntityView _entityView;
        
        private Slider _healthSlider;
        private int _maxHealthValue;
        private int _healthValue;
        private int _assumedHealthValue;

        public void Initialize(
            int healthValue,
            int maxHealthValue,
            Slider healthSlider,
            IEntityView entityView
        )
        {
            _healthValue = healthValue;
            _maxHealthValue = maxHealthValue;
            _healthSlider = healthSlider;
            _entityView = entityView;
            _assumedHealthValue = healthValue;
        }
        
        public void AddHealth(int amount)
        {
            _healthValue = Mathf.Clamp(_healthValue + amount, 0, _maxHealthValue);
            _healthSlider.value = (float)_healthValue / _maxHealthValue;
        }

        public void ReduceHealth(int amount)
        {
            _healthValue = Mathf.Clamp(_healthValue - amount, 0, _maxHealthValue);
            _healthSlider.value = (float)_healthValue / _maxHealthValue;
            if (_healthValue <= 0)
            {
                Die();
            }
        }

        public void ReduceAssumedHealth(int amount)
        {
            _assumedHealthValue -= amount;
        }

        public bool CheckAssumedStatus()
        {
            return _assumedHealthValue > 0;
        }
        
        protected virtual void Die() { }
        
        public int GetHealth()
        {
            return _healthValue;
        }
    }
}