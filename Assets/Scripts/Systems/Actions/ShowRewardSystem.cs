using System;
using System.Collections;
using Db;
using Helpers;
using Signals;
using UI.Views.Game;
using UniRx;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Systems.Actions
{
    public class ShowRewardSystem : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly Camera _camera;
        private readonly VisualEffectsSettings _visualEffects;
        private readonly SceneHandler _sceneHandler;
        
        public ShowRewardSystem(
            SignalBus signalBus,
            Camera gameCamera,
            VisualEffectsSettings visualEffects,
            SceneHandler sceneHandler
            )
        {
            _signalBus = signalBus;
            _camera = gameCamera;
            _visualEffects = visualEffects;
            _sceneHandler = sceneHandler;
        }

        private void SpawnRewardEffect(ShowRewardSignal signal)
        {
            var worldPos = signal.worldPos;
            var screenPoint = _camera.WorldToScreenPoint(worldPos);
            var rewardEffect = _visualEffects.RewardEffect;
            var parent = _sceneHandler.ParentForUiEffects;
            var rewardView = Object.Instantiate(rewardEffect, parent);
            var speedAnimating = _visualEffects.SpeedAnimating;
            var timeAnimating = _visualEffects.TimeShowReward;
            var text = rewardView.Text;

            text.text = $"+{signal.count.ToString()}";
            screenPoint.x -= 720;
            screenPoint.y -= 1520;
            rewardView.RectTransform.anchoredPosition = screenPoint;

            Observable.FromCoroutine(() => AnimateEffectWithDestroy(rewardView, speedAnimating, timeAnimating))
                .Subscribe();
        }

        private IEnumerator AnimateEffectWithDestroy(RewardView rewardView, float speedAnimating, float timeShow)
        {
            var time = 0f;
            var rectEffect = rewardView.RectTransform;
            var textEffect = rewardView.Text;
            var startColorText = textEffect.color;
            var targetColorText = startColorText;
            targetColorText.a = 0;
            
            do
            {
                var deltaTime = Time.deltaTime;
                time += deltaTime;
                rectEffect.anchoredPosition += Vector2.up * speedAnimating * deltaTime;
                textEffect.color = Color.Lerp(startColorText, targetColorText, time / timeShow);
                
                yield return null;
                
            } while (time < timeShow);
            
            Object.Destroy(rectEffect.gameObject);
        }
        
        public void Initialize()
        {
            _signalBus.Subscribe<ShowRewardSignal>(SpawnRewardEffect);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<ShowRewardSignal>(SpawnRewardEffect);
        }
    }
}