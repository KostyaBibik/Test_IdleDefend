using UnityEngine;
using UnityEngine.UI;

namespace Components
{
    public class HealthComponent : MonoBehaviour, IHealable
    {
        private int _maxHealthValue;
        private int _healthValue;
        public Slider _healthSlider;

        public void Initialize(
            int healthValue,
            int maxHealthValue,
            Slider healthSlider
        )
        {
            _healthValue = healthValue;
            _maxHealthValue = maxHealthValue;
            _healthSlider = healthSlider;
        }
        
        public void AddHealth(int amount)
        {
            _healthValue = Mathf.Clamp(_healthValue + amount, 0, _maxHealthValue);
            _healthSlider.value = (float)_healthValue / _maxHealthValue;
        }

        public void ReduceHealth(int amount)
        {
            
            Debug.Log($"{_healthValue} - {amount}");
            _healthValue = Mathf.Clamp(_healthValue - amount, 0, _maxHealthValue);
            _healthSlider.value = (float)_healthValue / _maxHealthValue;
        }

        public int GetHealth()
        {
            return _healthValue;
        }
    }
}