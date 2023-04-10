using System;
using System.Collections;
using Db;
using Signals;
using UniRx;
using UnityEngine;
using Zenject;
using Random = UnityEngine.Random;

namespace Systems.Actions
{
    public class ShakeCamOnDamageSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly Camera _camera;
        private readonly VisualEffectsSettings _visualEffectsSettings;
        
        private IDisposable _shakingObserver;
        private bool _isShaking;
        
        public ShakeCamOnDamageSystem(
            SignalBus signalBus,
            Camera mainCam,
            VisualEffectsSettings visualEffectsSettings
        )
        {
            _signalBus = signalBus;
            _camera = mainCam;
            _visualEffectsSettings = visualEffectsSettings;
        }

        private void ShakeCam(TowerLostHealthSignal signal)
        {
            if (_isShaking)
                _shakingObserver.Dispose();

            _shakingObserver = Observable.FromCoroutine(DoShake)
                .Subscribe();
        }
        
        private IEnumerator DoShake()
        {
            _isShaking = true;
            
            var elapsedTime = 0.0f;
            var camTransform = _camera.transform;
            var originalPos = camTransform.localPosition;
            var shakeDuration = _visualEffectsSettings.ShakeDuration;
            var shakeIntensity = _visualEffectsSettings.ShakeIntensity;
            
            while (elapsedTime < shakeDuration)
            {
                float x = Random.Range(-1f, 1f) * shakeIntensity;
                float y = Random.Range(-1f, 1f) * shakeIntensity;

                camTransform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

                elapsedTime += Time.deltaTime;

                yield return null;
            }

            camTransform.localPosition = originalPos;
            _isShaking = false;
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<TowerLostHealthSignal>(ShakeCam);
        }

        public void Dispose()
        { 
            _signalBus.Unsubscribe<TowerLostHealthSignal>(ShakeCam);
        }
    }
}