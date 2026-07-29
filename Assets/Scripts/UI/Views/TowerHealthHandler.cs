using System;
using System.Collections;
using System.Collections.Generic;
using UI.Views.Game;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class TowerHealthHandler : MonoBehaviour
    {
        [SerializeField] private float speedChangeColor = .5f;
        [SerializeField] private Color activeLife;
        [SerializeField] private Color disableLife;
        [SerializeField] private Transform contentRect;

        private List<Image> _lifeCounters = new List<Image>();
        private TowerHealthView _healthPrefab;

        private int _lifeCounter;
        private int _maxCounter;
        private bool _isLostAnimating;
        private bool _isAddAnimating;
        
        private IDisposable _addHealthObserver;
        private IDisposable _lostHealthObserver;
        
        public void InitializeHealths(
            int currentCount,
            int maxCount,
            TowerHealthView healthPrefab
        )
        {
            _maxCounter = maxCount;
            _healthPrefab = healthPrefab;
            
            for (var iterator = 0; iterator < maxCount; iterator++)
            {
                var healthView = CreateHealthView();
                
                if (iterator < currentCount)
                    AddHealth();
                else
                    healthView.Image.color = disableLife;
            }
        }

        public bool CanUpHealth()
        {
            return _lifeCounter < _maxCounter;
        }
        
        public void AddHealth()
        {
            if(_isLostAnimating)
                _lostHealthObserver.Dispose();
            
            _addHealthObserver = Observable.FromCoroutine(ActivateLife).Subscribe();
        }
        
        public void LoseHealth()
        {
            if(_isAddAnimating)
                _addHealthObserver.Dispose();
            
            _lostHealthObserver = Observable.FromCoroutine(DisableLife).Subscribe();
        }
        
        private IEnumerator DisableLife()
        {
            if(_lifeCounter <= 0)
                yield break;

            _isLostAnimating = true;
            _lifeCounter--;
            var activatedLife = _lifeCounters[_lifeCounter];
            var startColor = activatedLife.color;
            var targetColor = disableLife;
            var time = 0f;

            do
            {
                time += Time.deltaTime * speedChangeColor;
                time = Mathf.Clamp(time, 0f, 1f);
                activatedLife.color = Color.Lerp(startColor, targetColor, time);
                yield return null;
            } while (activatedLife.color != targetColor);
            
            _isLostAnimating = false;
        }
        
        private IEnumerator ActivateLife()
        {
            while (_maxCounter < _lifeCounter + 1)
                AddMaxHealthSlot();
            
            _lifeCounter++;
            _isAddAnimating = true;
            
            var activatedLife = _lifeCounters[_lifeCounter-1];
            var startColor = activatedLife.color;
            var targetColor = activeLife;
            var time = 0f;

            do
            {
                time += Time.deltaTime * speedChangeColor;
                time = Mathf.Clamp(time, 0f, 1f);
                activatedLife.color = Color.Lerp(startColor, targetColor, time);
                yield return null;
            } while (activatedLife.color != targetColor);
            
            _isAddAnimating = false;
        }

        private void AddMaxHealthSlot()
        {
            var healthView = CreateHealthView();
            healthView.Image.color = disableLife;
            _maxCounter++;
        }

        private TowerHealthView CreateHealthView()
        {
            var healthView = Instantiate(_healthPrefab, contentRect);
            _lifeCounters.Add(healthView.Image);
            return healthView;
        }
    }
}
