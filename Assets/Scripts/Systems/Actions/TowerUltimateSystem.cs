using System;
using System.Collections;
using System.Collections.Generic;
using Enums;
using Services;
using Services.Impl;
using Signals;
using UniRx;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Systems.Actions
{
    /// <summary>
    /// Фирменная ультимативная способность экипированной башни: кулдаун тикает через
    /// IGameTimeProvider (переживает паузу так же, как весь остальной бой), эффект зависит
    /// от TowerView.attackType — по одной ультиме на тип атаки.
    /// </summary>
    public class TowerUltimateSystem : ITickable
    {
        private readonly TowerView _towerView;
        private readonly EnemyService _enemyService;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly FreezeWaveSystem _freezeWaveSystem;
        private readonly SignalBus _signalBus;

        // Переиспользуемый буфер под точки Раскола: ульта срабатывает часто и по всем врагам,
        // новый список на каждую активацию тут ни к чему.
        private readonly List<Vector3> _shatterHitPoints = new();

        public TowerUltimateSystem(
            TowerView towerView,
            EnemyService enemyService,
            IGameTimeProvider gameTimeProvider,
            FreezeWaveSystem freezeWaveSystem,
            SignalBus signalBus
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _gameTimeProvider = gameTimeProvider;
            _freezeWaveSystem = freezeWaveSystem;
            _signalBus = signalBus;
        }

        public void Tick()
        {
            if (_towerView.ultimateCooldownRemaining > 0f)
                _towerView.ultimateCooldownRemaining -= _gameTimeProvider.DeltaTime;
        }

        public bool TryActivate()
        {
            // ultimateCooldown <= 0 значит TowerInitializeSystem не нашёл тело башни с ультимейтом
            // (пустой каталог/не заполненный товар) — кнопка в UI в этом случае тоже должна быть скрыта.
            if (_towerView.ultimateCooldown <= 0f)
                return false;

            if (_towerView.ultimateCooldownRemaining > 0f)
                return false;

            // Длительность нужна визуалу, чтобы понимать: держать ауру на время действия
            // или ограничиться разовой вспышкой.
            var duration = 0f;
            IReadOnlyList<Vector3> hitPoints = null;

            switch (_towerView.attackType)
            {
                case EMainTowerAttackType.Default:
                    ActivateBarrage();
                    duration = _towerView.barrageDuration;
                    break;
                case EMainTowerAttackType.Pierce:
                    hitPoints = ActivateShatter();
                    break;
                case EMainTowerAttackType.Frost:
                    ActivateFreeze();
                    duration = _towerView.freezeDuration;
                    break;
                case EMainTowerAttackType.Splash:
                    ActivateOverload();
                    duration = _towerView.overloadDuration;
                    break;
            }

            _signalBus.Fire(new TowerUltimateActivatedSignal
            {
                attackType = _towerView.attackType,
                duration = duration,
                worldPos = _towerView.transform.position,
                hitPoints = hitPoints
            });

            _towerView.ultimateCooldownRemaining = _towerView.ultimateCooldown;
            return true;
        }

        private void ActivateBarrage()
        {
            var multiplier = Mathf.Max(1f, _towerView.barrageAttackSpeedMultiplier);
            _towerView.ultimateAttackSpeedMultiplier = multiplier;

            Observable.FromCoroutine(() => RevertAfter(
                _towerView.barrageDuration,
                () => _towerView.ultimateAttackSpeedMultiplier = 1f)).Subscribe();
        }

        private IReadOnlyList<Vector3> ActivateShatter()
        {
            var percent = _towerView.shatterDamagePercentOfMaxHealth;
            _shatterHitPoints.Clear();

            foreach (var enemy in _enemyService.GetAssumedActiveEnemies())
            {
                var damage = Mathf.RoundToInt(enemy.healthComponent.GetMaxHealth() * percent);
                enemy.healthComponent.ReduceHealth(damage);
                _shatterHitPoints.Add(enemy.transform.position);
            }

            return _shatterHitPoints;
        }

        private void ActivateFreeze()
        {
            // Враги замерзают по фронту расширяющейся волны (FreezeWaveSystem), не мгновенно все
            // разом - гвард в EnemyView.ApplyFrost всё равно гарантирует, что 0 (полная остановка)
            // не будет ослаблен более слабым пассивным эффектом, догнавшим цель позже.
            _freezeWaveSystem.StartWave();
        }

        private void ActivateOverload()
        {
            var multiplier = _towerView.overloadSplashRadiusMultiplier;
            _towerView.splashRadius *= multiplier;

            Observable.FromCoroutine(() => RevertAfter(
                _towerView.overloadDuration,
                () => _towerView.splashRadius /= multiplier)).Subscribe();
        }

        private IEnumerator RevertAfter(float seconds, Action revert)
        {
            yield return _gameTimeProvider.WaitForSeconds(seconds);
            revert();
        }
    }
}
