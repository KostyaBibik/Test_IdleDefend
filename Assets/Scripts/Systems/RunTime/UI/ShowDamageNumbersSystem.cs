using System;
using System.Collections.Generic;
using Db;
using Helpers;
using Services;
using Signals;
using TMPro;
using UI.Views.Game;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Systems.RunTime.UI
{
    /// <summary>
    /// Всплывающие числа урона по TowerDamageDealtSignal.
    /// Пул объектов и один общий Tick вместо корутины на каждое число: при splash/pierce
    /// за кадр прилетает пачка попаданий, а проект собирается под WebGL - лишние
    /// Instantiate/Destroy и аллокации тут дороже всего.
    /// </summary>
    public class ShowDamageNumbersSystem : IInitializable, ITickable, IDisposable
    {
        // Приоритеты нужны только при переполнении экрана: чем ниже, тем раньше число
        // уступит место более важному попаданию.
        private const int PrioritySecondary = 0;
        private const int PriorityPrimary = 1;
        private const int PriorityCritical = 2;

        private const float PopPhase = 0.25f;
        private const float PopCritical = 0.35f;
        private const float PopNormal = 0.1f;

        private readonly SignalBus _signalBus;
        // Полное имя намеренно: внутри Systems.RunTime простое Camera - это неймспейс проекта.
        private readonly UnityEngine.Camera _camera;
        private readonly VisualEffectsSettings _visualEffects;
        private readonly SceneHandler _sceneHandler;
        private readonly IGameTimeProvider _gameTimeProvider;

        private readonly List<Entry> _active = new();
        private readonly Stack<RewardView> _pool = new();

        public ShowDamageNumbersSystem(
            SignalBus signalBus,
            UnityEngine.Camera gameCamera,
            VisualEffectsSettings visualEffects,
            SceneHandler sceneHandler,
            IGameTimeProvider gameTimeProvider
        )
        {
            _signalBus = signalBus;
            _camera = gameCamera;
            _visualEffects = visualEffects;
            _sceneHandler = sceneHandler;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<TowerDamageDealtSignal>(OnDamageDealt);
        }

        public void Tick()
        {
            if (_active.Count == 0)
                return;

            var deltaTime = _gameTimeProvider.DeltaTime;

            for (var i = _active.Count - 1; i >= 0; i--)
            {
                var entry = _active[i];

                // Сцена могла перезагрузиться под нами - тогда просто забываем запись.
                if (entry.View == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                entry.Elapsed += deltaTime;
                if (entry.Elapsed >= entry.Lifetime)
                {
                    Recycle(i);
                    continue;
                }

                var progress = entry.Elapsed / entry.Lifetime;

                entry.Position += entry.Velocity * deltaTime;
                entry.View.RectTransform.anchoredPosition = entry.Position;

                // Прозрачность растёт по квадрату: число держится читаемым почти весь путь
                // и гаснет резко в конце, а не размазывается в муть по всему экрану.
                var color = entry.StartColor;
                color.a = entry.StartColor.a * (1f - progress * progress);
                entry.View.Text.color = color;

                var pop = Mathf.Lerp(entry.PopAmount, 0f, Mathf.Clamp01(progress / PopPhase));
                entry.View.RectTransform.localScale = Vector3.one * (1f + pop);

                _active[i] = entry;
            }
        }

        private void OnDamageDealt(TowerDamageDealtSignal signal)
        {
            if (signal.damage <= 0)
                return;

            var prefab = _visualEffects.DamageNumberEffect;
            var parent = _sceneHandler != null ? _sceneHandler.ParentForUiEffects : null;
            if (prefab == null || parent == null || _camera == null)
                return;

            var priority = signal.isCritical
                ? PriorityCritical
                : signal.isPrimaryHit
                    ? PriorityPrimary
                    : PrioritySecondary;

            if (!TryReserveSlot(priority))
                return;

            // target к этому моменту может быть уже уничтожен, поэтому позиция берётся
            // из сигнала, а не из target.transform.
            var screenPoint = _camera.WorldToScreenPoint(signal.worldPos);
            if (screenPoint.z < 0f)
                return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screenPoint, null, out var localPoint);

            var view = Rent(prefab, parent);
            if (view == null)
                return;

            var spread = _visualEffects.DamageNumberSpread;
            var isSecondary = !signal.isPrimaryHit;

            var fontSize = signal.isCritical ? _visualEffects.CriticalFontSize : _visualEffects.DamageNumberFontSize;
            var color = signal.isCritical ? _visualEffects.CriticalColor : _visualEffects.DamageNumberColor;
            var lifetime = _visualEffects.DamageNumberLifetime;

            if (isSecondary)
            {
                // Splash/pierce/chain приглушаем, иначе один выстрел засыпает пол-арены числами.
                fontSize *= _visualEffects.SecondaryHitScale;
                color.a *= _visualEffects.SecondaryHitAlpha;
                lifetime *= 0.75f;
            }

            var text = view.Text;
            text.text = signal.isCritical
                ? signal.damage.ToString() + _visualEffects.CriticalSuffix
                : signal.damage.ToString();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;

            var entry = new Entry
            {
                View = view,
                Elapsed = 0f,
                Lifetime = Mathf.Max(0.05f, lifetime),
                Position = localPoint + new Vector2(Random.Range(-spread, spread) * 0.5f, Random.Range(-spread, spread) * 0.2f),
                Velocity = new Vector2(Random.Range(-spread, spread) * 0.35f, _visualEffects.DamageNumberRiseSpeed),
                StartColor = color,
                PopAmount = signal.isCritical ? PopCritical : PopNormal,
                Priority = priority
            };

            view.RectTransform.anchoredPosition = entry.Position;
            view.RectTransform.localScale = Vector3.one * (1f + entry.PopAmount);

            _active.Add(entry);
        }

        /// <summary>
        /// Держит потолок одновременных чисел. При переполнении новое попадание вытесняет
        /// самое старое из менее важных, а если вытеснять нечего - просто не показывается.
        /// </summary>
        private bool TryReserveSlot(int priority)
        {
            var limit = Mathf.Max(1, _visualEffects.DamageNumberMaxConcurrent);
            if (_active.Count < limit)
                return true;

            var victim = -1;
            for (var i = 0; i < _active.Count; i++)
            {
                if (_active[i].Priority >= priority)
                    continue;

                // Список упорядочен по времени появления, поэтому первый подходящий - самый старый.
                victim = i;
                break;
            }

            if (victim < 0)
                return false;

            Recycle(victim);
            return true;
        }

        private RewardView Rent(RewardView prefab, RectTransform parent)
        {
            while (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                if (pooled == null)
                    continue;

                pooled.transform.SetParent(parent, false);
                pooled.gameObject.SetActive(true);
                return pooled;
            }

            return Object.Instantiate(prefab, parent);
        }

        private void Recycle(int index)
        {
            var view = _active[index].View;
            _active.RemoveAt(index);

            if (view == null)
                return;

            view.gameObject.SetActive(false);
            _pool.Push(view);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<TowerDamageDealtSignal>(OnDamageDealt);

            foreach (var entry in _active)
            {
                if (entry.View != null)
                    Object.Destroy(entry.View.gameObject);
            }

            _active.Clear();

            while (_pool.Count > 0)
            {
                var pooled = _pool.Pop();
                if (pooled != null)
                    Object.Destroy(pooled.gameObject);
            }
        }

        private struct Entry
        {
            public RewardView View;
            public float Elapsed;
            public float Lifetime;
            public Vector2 Position;
            public Vector2 Velocity;
            public Color StartColor;
            public float PopAmount;
            public int Priority;
        }
    }
}
