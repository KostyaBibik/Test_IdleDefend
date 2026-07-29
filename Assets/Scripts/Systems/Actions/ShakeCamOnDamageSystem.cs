using System;
using System.Collections;
using Db;
using Services;
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
        private readonly IGameTimeProvider _gameTimeProvider;

        private IDisposable _shakingObserver;
        private bool _isShaking;

        // Смещение, которое тряска сейчас держит на камере. Работаем именно смещением, а не
        // снимком позиции: CameraZoomSystem параллельно ведёт камеру по Y, и восстановление
        // старого снимка отматывало бы её работу назад.
        private Vector3 _appliedOffset;

        public ShakeCamOnDamageSystem(
            SignalBus signalBus,
            Camera mainCam,
            VisualEffectsSettings visualEffectsSettings,
            IGameTimeProvider gameTimeProvider
        )
        {
            _signalBus = signalBus;
            _camera = mainCam;
            _visualEffectsSettings = visualEffectsSettings;
            _gameTimeProvider = gameTimeProvider;
        }

        private void ShakeCam(TowerLostHealthSignal signal)
        {
            // Прошлую тряску обрываем принудительно, её собственный финал не отработает,
            // поэтому смещение снимаем здесь - иначе камера уползала бы с каждым попаданием.
            StopShake();

            _shakingObserver = Observable.FromCoroutine(DoShake)
                .Subscribe();
        }

        private IEnumerator DoShake()
        {
            _isShaking = true;

            var elapsedTime = 0.0f;
            var shakeDuration = _visualEffectsSettings.ShakeDuration;
            var shakeIntensity = _visualEffectsSettings.ShakeIntensity;

            while (elapsedTime < shakeDuration)
            {
                var deltaTime = _gameTimeProvider.DeltaTime;

                // На паузе время стоит, а на экране проигрыша - навсегда. Раньше цикл в этот
                // момент не мог досчитать до shakeDuration, но продолжал каждый кадр кидать
                // камеру в случайную точку - отсюда бесконечное дрожание за окном проигрыша.
                if (deltaTime <= 0f)
                    break;

                var offset = new Vector3(
                    Random.Range(-1f, 1f) * shakeIntensity,
                    Random.Range(-1f, 1f) * shakeIntensity,
                    0f);

                ApplyOffset(offset);

                elapsedTime += deltaTime;

                yield return null;
            }

            StopShake();
        }

        private void ApplyOffset(Vector3 offset)
        {
            if (_camera == null)
                return;

            var camTransform = _camera.transform;
            camTransform.localPosition += offset - _appliedOffset;
            _appliedOffset = offset;
        }

        /// <summary>
        /// Гасит тряску и возвращает камере её собственную позицию. Безопасно вызывать
        /// повторно и когда тряски нет.
        /// </summary>
        private void StopShake()
        {
            _shakingObserver?.Dispose();
            _shakingObserver = null;

            ApplyOffset(Vector3.zero);
            _isShaking = false;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<TowerLostHealthSignal>(ShakeCam);
        }

        public void Dispose()
        {
            StopShake();

            _signalBus.Unsubscribe<TowerLostHealthSignal>(ShakeCam);
        }
    }
}
