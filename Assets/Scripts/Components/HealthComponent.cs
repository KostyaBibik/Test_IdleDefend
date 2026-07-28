using UnityEngine;
using UnityEngine.UI;
using Views;

namespace Components
{
    public abstract class HealthComponent : MonoBehaviour, IHealable
    {
        protected IEntityView _entityView;
        protected bool _hasReward = true;
        
        private Slider _healthSlider;
        private int _maxHealthValue;
        private int _healthValue;
        private int _reservedIncomingDamage;

        public void Initialize(
            int healthValue,
            Slider healthSlider,
            IEntityView entityView
        )
        {
            _healthValue = healthValue;
            _maxHealthValue = healthValue;
            _healthSlider = healthSlider;
            _entityView = entityView;
            _reservedIncomingDamage = 0;
        }
        
        public void AddHealth(int amount)
        {
            _healthValue = Mathf.Clamp(_healthValue + amount, 0, _maxHealthValue);
            _healthSlider.value = (float)_healthValue / _maxHealthValue;
        }

        public virtual void ReduceHealth(int amount)
        {
            var damage = Mathf.Max(0, amount);
            _healthValue = Mathf.Clamp(_healthValue - damage, 0, _maxHealthValue);
            _reservedIncomingDamage = Mathf.Max(0, _reservedIncomingDamage - damage);
            _healthSlider.value = (float)_healthValue / _maxHealthValue;
            if (_healthValue <= 0)
            {
                Die();
            }
        }

        public void DestroyOnAttackTower()
        {
            _hasReward = false;
            Die();
        }

        public void ReduceAssumedHealth(int amount)
        {
            _reservedIncomingDamage = Mathf.Clamp(
                _reservedIncomingDamage + GetEffectiveDamage(amount),
                0,
                _healthValue);
        }

        public bool CheckAssumedStatus()
        {
            return _healthValue - _reservedIncomingDamage > 0;
        }

        public virtual int GetEffectiveDamage(int amount)
        {
            return amount;
        }
        
        protected virtual void Die()
        {
            _entityView.isDestroyed = true;
        }
        
        public int GetHealth()
        {
            return _healthValue;
        }

        public int GetMaxHealth()
        {
            return _maxHealthValue;
        }
    }
}
