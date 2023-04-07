using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class TowerHealthView : MonoBehaviour
    {
        [SerializeField] private float speedChangeColor = .5f;
        [SerializeField] private Color activeLife;
        [SerializeField] private Color disableLife;

        [SerializeField] private List<Image> lifeCounters;
        [SerializeField] private Image extraLife;

        private int _lifeCounter;
        
        public void AddHealth()
        {
            Observable.FromCoroutine(ActivateLife)
                .Subscribe();
        }
        
        public void LoseHealth()
        {
            Observable.FromCoroutine(DisableLife)
                .Subscribe();
        }

        public void ResetHealth(int totalCount)
        {
            DisableAllHealths();
            
            for (var i = 0; i < totalCount; i++)
            {
                AddHealth();
            }
        }

        private void DisableAllHealths()
        {
            _lifeCounter = 0;
            
            foreach (var life in lifeCounters)
            {
                life.color = disableLife;
            }
            extraLife.gameObject.SetActive(false);
        }

        private IEnumerator DisableLife()
        {
            _lifeCounter--;
            var activatedLife = lifeCounters[_lifeCounter];
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
        }
        
        private IEnumerator ActivateLife()
        {
            _lifeCounter++;
            var activatedLife = lifeCounters[_lifeCounter-1];
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
        }
    }
}