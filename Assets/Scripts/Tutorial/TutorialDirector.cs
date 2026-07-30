using System;
using System.Collections.Generic;
using Db;
using Enums;
using Infrastructure.Impl;
using Services;
using Signals;
using Systems.RunTime.Camera;
using UI.Views;
using UnityEngine;
using Views.Impl;
using Zenject;

namespace Tutorial
{
    /// <summary>
    /// Drives the deterministic opening of the first level. Visual prompts subscribe to phase
    /// changes separately, keeping scenario rules independent from presentation.
    /// </summary>
    public sealed class TutorialDirector : IInitializable, ITickable, IDisposable
    {
        private readonly TutorialScenarioConfig _config;
        private readonly TutorialRuntimeState _runtime;
        private readonly LevelService _levelService;
        private readonly IGameTimeProvider _gameTimeProvider;
        private readonly EntityFactory _entityFactory;
        private readonly TowerView _towerView;
        private readonly SignalBus _signalBus;
        private readonly CameraZoomSystem _cameraZoomSystem;
        private readonly SideTowerSlotService _sideTowerSlotService;
        private readonly List<EnemyView> _showcaseEnemies = new();

        private float _phaseTimer;
        private bool _celebrationStarted;
        private bool _experiencePausePending;
        private bool _ultimatePausePending;
        private bool _isSubscribed;
        private EnemyView _introEnemy;
        private int _introEnemiesDefeated;

        public Vector3 FirstEnemyDeathPosition { get; private set; }
        public SideTowerSlotMarkerView TutorialSideTowerMarker { get; private set; }

        public TutorialDirector(
            TutorialScenarioConfig config,
            TutorialRuntimeState runtime,
            LevelService levelService,
            IGameTimeProvider gameTimeProvider,
            EntityFactory entityFactory,
            TowerView towerView,
            SignalBus signalBus,
            CameraZoomSystem cameraZoomSystem,
            SideTowerSlotService sideTowerSlotService)
        {
            _config = config;
            _runtime = runtime;
            _levelService = levelService;
            _gameTimeProvider = gameTimeProvider;
            _entityFactory = entityFactory;
            _towerView = towerView;
            _signalBus = signalBus;
            _cameraZoomSystem = cameraZoomSystem;
            _sideTowerSlotService = sideTowerSlotService;
        }

        public void Initialize()
        {
            if (!ShouldRun())
                return;

            _signalBus.Subscribe<TowerUpgradePurchasedSignal>(OnUpgradePurchased);
            _signalBus.Subscribe<DestroyEntitySignal>(OnEntityDestroyed);
            _signalBus.Subscribe<TowerExperienceDroppedSignal>(OnExperienceDropped);
            _signalBus.Subscribe<TowerBuffSelectedSignal>(OnBuffSelected);
            _signalBus.Subscribe<TowerUltimateActivatedSignal>(OnUltimateActivated);
            _signalBus.Subscribe<SideTowerPickerOpenedSignal>(OnSideTowerPickerOpened);
            _signalBus.Subscribe<SideTowerPurchasedSignal>(OnSideTowerPurchased);
            _signalBus.Subscribe<TutorialPhaseChangedSignal>(OnPhaseChangedAnalytics);
            _isSubscribed = true;

            GaEventProvider.DesignEvent("Tutorial:Started");
            _runtime.Begin();
            _introEnemiesDefeated = 0;
            SpawnIntroEnemy();
            _gameTimeProvider.Resume();
        }

        private bool ShouldRun()
        {
            if (_config == null || !_config.Enabled || !_config.HasValidBuffChoices())
                return false;

            if (_levelService.CurrentLevelIndex != _config.LevelIndex)
                return false;

            var isFreshPlayer = SaveSystem.SaveData.TutorialVersion < _config.Version
                                && SaveSystem.GetLevelStars(_levelService.CurrentLevel.LevelId) <= 0;
            return isFreshPlayer || (Application.isEditor && _config.ForceInEditor);
        }

        private void SpawnIntroEnemy()
        {
            var attackRange = Mathf.Max(0.5f, _towerView.attackDistance * _towerView.ratioRange);
            var spawnDistance = attackRange * Mathf.Clamp(_config.FirstEnemyRangeRatio, 0.5f, 1f);
            var angle = 90f + _introEnemiesDefeated * 55f;
            var direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;

            _introEnemy = _entityFactory.CreateEnemy(
                _towerView.transform.position + direction * spawnDistance,
                _config.FirstEnemyType,
                _config.FirstEnemyExtraHealth,
                0f,
                -1,
                grantCoinReward: false,
                grantExperienceReward: false);
        }

        private void OnEntityDestroyed(DestroyEntitySignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.IntroCombat)
                return;

            if (signal.view != _introEnemy)
                return;

            _introEnemy = null;
            _introEnemiesDefeated++;

            if (_introEnemiesDefeated < _config.IntroEnemyCount)
            {
                SpawnIntroEnemy();
                return;
            }

            _runtime.SetPhase(ETutorialPhase.DamageUpgrade);
            _gameTimeProvider.Pause();
        }

        private void OnUpgradePurchased(TowerUpgradePurchasedSignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.DamageUpgrade)
                return;

            if (signal.upgradeType != EUpgradeType.AttackDamage)
                return;

            _runtime.SetPhase(ETutorialPhase.FirstEnemy);
            SpawnFirstEnemy();
            _gameTimeProvider.Resume();
        }

        private void SpawnFirstEnemy()
        {
            var attackRange = Mathf.Max(0.5f, _towerView.attackDistance * _towerView.ratioRange);
            var spawnDistance = attackRange * Mathf.Clamp(_config.FirstEnemyRangeRatio, 0.5f, 1f);
            var spawnPosition = _towerView.transform.position + Vector3.up * spawnDistance;

            _entityFactory.CreateEnemy(
                spawnPosition,
                _config.FirstEnemyType,
                _config.FirstEnemyExtraHealth,
                0f,
                _config.FirstEnemyExperience);
        }

        private void OnExperienceDropped(TowerExperienceDroppedSignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.FirstEnemy)
                return;

            if (signal.amount != _config.FirstEnemyExperience)
                return;

            FirstEnemyDeathPosition = signal.worldPosition;
            _runtime.SetPhase(ETutorialPhase.ExperienceExplanation);
            _gameTimeProvider.SetTimeScale(_config.ExplanationTimeScale);
            _phaseTimer = 0.05f;
            _experiencePausePending = true;
        }

        public void ContinueExperienceExplanation()
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.ExperienceExplanation)
                return;

            _runtime.SetPhase(ETutorialPhase.ExperienceTravel);
            _gameTimeProvider.Resume();
        }

        private void OnBuffSelected(TowerBuffSelectedSignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.BuffSelection)
                return;

            if (signal.buff != _config.HighlightedLegendaryBuff)
                return;

            _runtime.SetPhase(ETutorialPhase.MultishotShowcase);
            SpawnShowcaseWave();
            _phaseTimer = _config.MultishotPreviewSeconds;
        }

        public void Tick()
        {
            if (!_runtime.IsRunning)
                return;

            RemoveDestroyedShowcaseEnemies();

            if (_runtime.Phase == ETutorialPhase.ExperienceExplanation && _experiencePausePending)
            {
                _phaseTimer -= _gameTimeProvider.DeltaTime;
                if (_phaseTimer <= 0f)
                {
                    _experiencePausePending = false;
                    _gameTimeProvider.Pause();
                }
            }

            if (_runtime.Phase == ETutorialPhase.UltimateShowcase && _ultimatePausePending)
            {
                _phaseTimer -= _gameTimeProvider.DeltaTime;
                if (_phaseTimer <= 0f)
                {
                    _ultimatePausePending = false;
                    _gameTimeProvider.Pause();
                }
            }

            switch (_runtime.Phase)
            {
                case ETutorialPhase.MultishotShowcase:
                    _phaseTimer -= _gameTimeProvider.DeltaTime;
                    if (_phaseTimer <= 0f)
                    {
                        if (_showcaseEnemies.Count == 0)
                            SpawnShowcaseWave();

                        _runtime.SetPhase(ETutorialPhase.UltimateShowcase);
                        _gameTimeProvider.SetTimeScale(_config.ExplanationTimeScale);
                        _phaseTimer = 0.1f;
                        _ultimatePausePending = true;
                    }
                    break;
                case ETutorialPhase.UltimateResolution:
                    TickUltimateResolution();
                    break;
            }
        }

        private void SpawnShowcaseWave()
        {
            _showcaseEnemies.Clear();
            SpawnRing(
                _config.ShowcaseEnemyType,
                _config.ShowcaseEnemyCount,
                _config.ShowcaseEnemyExtraHealth,
                _config.ShowcaseEnemyExtraSpeed,
                _showcaseEnemies);
        }

        private void OnUltimateActivated(TowerUltimateActivatedSignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.UltimateShowcase)
                return;

            _ultimatePausePending = false;

            foreach (var enemy in _showcaseEnemies)
            {
                if (enemy != null)
                    enemy.tutorialDamageTakenMultiplier = _config.UltimateDamageMultiplier;
            }

            _celebrationStarted = false;
            _runtime.SetPhase(ETutorialPhase.UltimateResolution);
            _gameTimeProvider.Resume();
        }

        private void TickUltimateResolution()
        {
            if (_showcaseEnemies.Count > 0)
                return;

            if (!_celebrationStarted)
            {
                _celebrationStarted = true;
                _phaseTimer = _config.CelebrationSeconds;
                return;
            }

            _phaseTimer -= _gameTimeProvider.DeltaTime;
            if (_phaseTimer > 0f)
                return;

            BeginSideTowerIntroduction();
        }

        private void BeginSideTowerIntroduction()
        {
            SpawnRing(
                _config.SideTowerWaveEnemyType,
                _config.SideTowerWaveEnemyCount,
                _config.SideTowerWaveExtraHealth,
                0f,
                null);

            TutorialSideTowerMarker = _sideTowerSlotService.SpawnTutorialMarker(0);
            _runtime.SetPhase(ETutorialPhase.SideTowerSlot);
            _gameTimeProvider.Pause();
        }

        private void OnSideTowerPickerOpened(SideTowerPickerOpenedSignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.SideTowerSlot)
                return;

            if (signal.marker != TutorialSideTowerMarker)
                return;

            _runtime.SetPhase(ETutorialPhase.SideTowerChoice);
        }

        private void OnSideTowerPurchased(SideTowerPurchasedSignal signal)
        {
            if (!_runtime.IsRunning || _runtime.Phase != ETutorialPhase.SideTowerChoice)
                return;

            if (signal.definition != _config.TutorialSideTower)
                return;

            _gameTimeProvider.Resume();
            _runtime.ReleaseGameplay();
            if (!(Application.isEditor && _config.ForceInEditor))
            {
                SaveSystem.SaveData.TutorialVersion = _config.Version;
                SaveSystem.Instance.SaveToStorage();
            }
            GaEventProvider.DesignEvent("Tutorial:Completed");
            _runtime.Complete();
        }

        private static void OnPhaseChangedAnalytics(TutorialPhaseChangedSignal signal)
        {
            GaEventProvider.DesignEvent($"Tutorial:Step:{signal.current}");
        }

        private void SpawnRing(EEnemyType type, int count, int extraHealth, float extraSpeed,
            ICollection<EnemyView> result)
        {
            count = Mathf.Max(1, count);
            var radius = Mathf.Max(
                _cameraZoomSystem.GetSafeSpawnRadius(),
                _towerView.attackDistance * _towerView.ratioRange * 1.35f);

            for (var i = 0; i < count; i++)
            {
                var angle = 90f + i * 360f / count;
                var direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                var enemy = _entityFactory.CreateEnemy(
                    _towerView.transform.position + direction * radius,
                    type,
                    extraHealth,
                    extraSpeed,
                    -1,
                    grantCoinReward: false,
                    grantExperienceReward: false);
                result?.Add(enemy);
            }
        }

        private void RemoveDestroyedShowcaseEnemies()
        {
            for (var i = _showcaseEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _showcaseEnemies[i];
                if (enemy == null || enemy.isDestroyed)
                    _showcaseEnemies.RemoveAt(i);
            }
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                _signalBus.Unsubscribe<TowerUpgradePurchasedSignal>(OnUpgradePurchased);
                _signalBus.Unsubscribe<DestroyEntitySignal>(OnEntityDestroyed);
                _signalBus.Unsubscribe<TowerExperienceDroppedSignal>(OnExperienceDropped);
                _signalBus.Unsubscribe<TowerBuffSelectedSignal>(OnBuffSelected);
                _signalBus.Unsubscribe<TowerUltimateActivatedSignal>(OnUltimateActivated);
                _signalBus.Unsubscribe<SideTowerPickerOpenedSignal>(OnSideTowerPickerOpened);
                _signalBus.Unsubscribe<SideTowerPurchasedSignal>(OnSideTowerPurchased);
                _signalBus.Unsubscribe<TutorialPhaseChangedSignal>(OnPhaseChangedAnalytics);
                _isSubscribed = false;
            }

            if (_runtime.IsRunning)
            {
                _runtime.Cancel();
                _gameTimeProvider.Resume();
            }
        }
    }
}
