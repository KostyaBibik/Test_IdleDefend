using System;
using System.Collections.Generic;
using Enums;
using Services;
using Signals;
using Systems.RunTime.Bullets;
using UnityEngine;
using Views.Impl;
using Zenject;
using Object = UnityEngine.Object;

namespace Systems.Actions
{
    /// <summary>
    /// Показывает, что ультимейт башни сработал и действует. Раньше это было видно только у
    /// Заморозки (её кольцо — часть механики), а Барраж, Раскол и Перегрузка меняли только
    /// числа в TowerView, поэтому на экране не происходило ничего.
    ///
    /// Что именно спавнить, задаётся в TowerBodyDefinition, а не в коде: каждому телу свой
    /// префаб ауры и вспышки.
    /// </summary>
    public class TowerUltimateVfxSystem : IInitializable, ITickable, IDisposable
    {
        private const float BurstLifetime = 2f;

        /// <summary>
        /// Осколочные лучи Раскола держатся дольше обычного пробития: ульта бьёт разом по всем,
        /// и игрок должен успеть прочитать, кого накрыло.
        /// </summary>
        private const float ShatterLineDuration = 0.45f;

        private readonly SignalBus _signalBus;
        private readonly TowerView _towerView;
        private readonly IGameTimeProvider _gameTimeProvider;

        // Точки Раскола, разложенные «звездой»: башня -> враг -> башня -> враг ...
        // Один LineRenderer не умеет рисовать разрывные отрезки, а возврат в центр даёт
        // ровно тот же результат без единого лишнего объекта на сцене.
        private readonly List<Vector3> _starPoints = new();

        private GameObject _activeAura;
        private float _auraRemaining;

        public TowerUltimateVfxSystem(
            SignalBus signalBus,
            TowerView towerView,
            IGameTimeProvider gameTimeProvider
        )
        {
            _signalBus = signalBus;
            _towerView = towerView;
            _gameTimeProvider = gameTimeProvider;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<TowerUltimateActivatedSignal>(OnUltimateActivated);
        }

        public void Tick()
        {
            if (_activeAura == null)
                return;

            _auraRemaining -= _gameTimeProvider.DeltaTime;
            if (_auraRemaining > 0f)
                return;

            DestroyAura();
        }

        private void OnUltimateActivated(TowerUltimateActivatedSignal signal)
        {
            SpawnBurst(signal.worldPos);

            if (signal.duration > 0f)
                SpawnAura(signal.duration);

            if (signal.attackType == EMainTowerAttackType.Pierce)
                ShowShatterBeams(signal);
        }

        private void SpawnBurst(Vector3 worldPos)
        {
            var prefab = _towerView.ultimateBurstPrefab;
            if (prefab == null)
                return;

            var instance = Object.Instantiate(prefab, worldPos, Quaternion.identity);
            PrepareInstance(instance);
            Object.Destroy(instance, BurstLifetime);
        }

        private void SpawnAura(float duration)
        {
            var prefab = _towerView.ultimateAuraPrefab;
            if (prefab == null)
                return;

            // Кулдаун ульты заметно длиннее её действия, так что перекрытие маловероятно,
            // но если ульту как-то запустят повторно — старую ауру убираем, а не копим.
            DestroyAura();

            var towerTransform = _towerView.transform;
            _activeAura = Object.Instantiate(prefab, towerTransform.position, Quaternion.identity, towerTransform);
            PrepareInstance(_activeAura);

            _auraRemaining = duration;
        }

        private void ShowShatterBeams(TowerUltimateActivatedSignal signal)
        {
            var hitPoints = signal.hitPoints;
            if (hitPoints == null || hitPoints.Count == 0)
                return;

            _starPoints.Clear();
            for (var i = 0; i < hitPoints.Count; i++)
            {
                _starPoints.Add(hitPoints[i]);

                // Возврат в центр между целями — кроме последней, иначе линия закончится
                // «хвостом» в башню.
                if (i < hitPoints.Count - 1)
                    _starPoints.Add(signal.worldPos);
            }

            BulletImpactVfx.ShowPierceLine(_towerView, signal.worldPos, _starPoints, ShatterLineDuration);
        }

        private void PrepareInstance(GameObject instance)
        {
            var scale = Mathf.Max(0.01f, _towerView.ultimateVfxScale);
            instance.transform.localScale = Vector3.one * scale;

            // Партиклы Epic Toon FX несут собственный AudioSource с playOnAwake — в проекте
            // звук у них глушится везде (см. BulletImpactVfx, FreezeWaveSystem).
            var audioSource = instance.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.enabled = false;
            }
        }

        private void DestroyAura()
        {
            if (_activeAura != null)
                Object.Destroy(_activeAura);

            _activeAura = null;
            _auraRemaining = 0f;
        }

        public void Dispose()
        {
            DestroyAura();

            _signalBus.Unsubscribe<TowerUltimateActivatedSignal>(OnUltimateActivated);
        }
    }
}
