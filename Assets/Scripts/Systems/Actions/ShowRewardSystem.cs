using System;
using System.Collections;
using Db;
using Helpers;
using Signals;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Systems.Actions
{
    public class ShowRewardSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly Camera _camera;
        private readonly VisualEffectsConfigSettings _visualEffectsConfig;
        private readonly SceneHandler _sceneHandler;
        
        public ShowRewardSystem(
            SignalBus signalBus,
            Camera gameCamera,
            VisualEffectsConfigSettings visualEffectsConfig,
            SceneHandler sceneHandler
            )
        {
            _signalBus = signalBus;
            _camera = gameCamera;
            _visualEffectsConfig = visualEffectsConfig;
            _sceneHandler = sceneHandler;
        }

        private void SpawnRewardEffect(ShowRewardOnFieldSignal signal)
        {
            var worldPos = signal.worldPos;
            var screenPoint = _camera.WorldToViewportPoint(worldPos);
            var prefabEffect = _visualEffectsConfig.RewardEffect;
            var parent = _sceneHandler.ParentForUiEffects;
            var effect = Object.Instantiate(prefabEffect, parent);
            effect.transform.position = screenPoint;
            
            Object.Destroy(effect, 10f);
        }

        private IEnumerator AnimateEffect(GameObject animatedObj)
        {
            
            
            yield break;
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<ShowRewardOnFieldSignal>(SpawnRewardEffect);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<ShowRewardOnFieldSignal>(SpawnRewardEffect);
        }
    }
}