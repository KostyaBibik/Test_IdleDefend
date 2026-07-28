using System;
using System.Collections;
using Enums;
using Services;
using Services.Impl;
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

        public TowerUltimateSystem(
            TowerView towerView,
            EnemyService enemyService,
            IGameTimeProvider gameTimeProvider,
            FreezeWaveSystem freezeWaveSystem
        )
        {
            _towerView = towerView;
            _enemyService = enemyService;
            _gameTimeProvider = gameTimeProvider;
            _freezeWaveSystem = freezeWaveSystem;
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

            switch (_towerView.attackType)
            {
                case EMainTowerAttackType.Default:
                    ActivateBarrage();
                    break;
                case EMainTowerAttackType.Pierce:
                    ActivateShatter();
                    break;
                case EMainTowerAttackType.Frost:
                    ActivateFreeze();
                    break;
                case EMainTowerAttackType.Splash:
                    ActivateOverload();
                    break;
            }

            _towerView.ultimateCooldownRemaining = _towerView.ultimateCooldown;
            return true;
        }

        private void ActivateBarrage()
        {
            var multiplier = _towerView.barrageAttackSpeedMultiplier;
            _towerView.attackSpeed *= multiplier;

            Observable.FromCoroutine(() => RevertAfter(
                _towerView.barrageDuration,
                () => _towerView.attackSpeed /= multiplier)).Subscribe();
        }

        private void ActivateShatter()
        {
            var percent = _towerView.shatterDamagePercentOfMaxHealth;

            foreach (var enemy in _enemyService.GetAssumedActiveEnemies())
            {
                var damage = Mathf.RoundToInt(enemy.healthComponent.GetMaxHealth() * percent);
                enemy.healthComponent.ReduceHealth(damage);
            }
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
