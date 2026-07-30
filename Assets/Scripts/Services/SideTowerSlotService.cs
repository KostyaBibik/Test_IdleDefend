using Db;
using Helpers;
using Infrastructure.Impl;
using System.Collections.Generic;
using UI.Views;
using UnityEngine;
using Zenject;
using Signals;
using Tutorial;

namespace Services
{
    public class SideTowerSlotService : IInitializable
    {
        private readonly LevelService _levelService;
        private readonly SceneHandler _sceneHandler;
        private readonly SideTowerCatalogConfig _catalog;
        private readonly CoinService _coinService;
        private readonly EntityFactory _entityFactory;
        private readonly Camera _camera;
        private readonly SignalBus _signalBus;
        private readonly TutorialRuntimeState _tutorialRuntime;
        private readonly Dictionary<int, SideTowerSlotMarkerView> _spawnedMarkers = new();

        public SideTowerSlotService(
            LevelService levelService,
            SceneHandler sceneHandler,
            SideTowerCatalogConfig catalog,
            CoinService coinService,
            EntityFactory entityFactory,
            Camera camera,
            SignalBus signalBus,
            TutorialRuntimeState tutorialRuntime
        )
        {
            _levelService = levelService;
            _sceneHandler = sceneHandler;
            _catalog = catalog;
            _coinService = coinService;
            _entityFactory = entityFactory;
            _camera = camera;
            _signalBus = signalBus;
            _tutorialRuntime = tutorialRuntime;
        }

        public void Initialize()
        {
            if (_tutorialRuntime.BlocksStandardGameplay)
                return;

            SpawnRegularMarkers();
        }

        private void SpawnRegularMarkers()
        {
            var indices = _levelService.CurrentLevel.UnlockedSideTowerSlotIndices;
            if (indices == null)
                return;

            var markers = _sceneHandler.SideTowerSlotMarkers;

            foreach (var index in indices)
            {
                if (index < 0 || markers == null || index >= markers.Length)
                    continue;

                SpawnMarker(index, markers[index].position);
            }
        }

        public SideTowerSlotMarkerView SpawnTutorialMarker(int index)
        {
            var markers = _sceneHandler.SideTowerSlotMarkers;
            if (index < 0 || markers == null || index >= markers.Length)
                return null;

            if (_spawnedMarkers.TryGetValue(index, out var existing) && existing != null)
                return existing;

            return SpawnMarker(index, markers[index].position);
        }

        private SideTowerSlotMarkerView SpawnMarker(int index, Vector3 position)
        {
            var marker = Object.Instantiate(_catalog.SlotMarkerPrefab, position, Quaternion.identity);
            marker.SetEventCamera(_camera);
            marker.Button.onClick.AddListener(() => OnMarkerClicked(marker));
            _spawnedMarkers[index] = marker;
            return marker;
        }

        private void OnMarkerClicked(SideTowerSlotMarkerView marker)
        {
            _sceneHandler.SideTowerPickerView.Show(marker.transform.position, _camera, _catalog.Definitions,
                definition => TryPurchase(marker, definition));
            _signalBus.Fire(new SideTowerPickerOpenedSignal { marker = marker });
        }

        private void TryPurchase(SideTowerSlotMarkerView marker, SideTowerDefinition definition)
        {
            if (!_coinService.TryBought(definition.Cost))
                return;

            _entityFactory.CreateSideTower(marker.transform.position, definition);
            _signalBus.Fire(new SideTowerPurchasedSignal
            {
                definition = definition,
                paidCost = definition.Cost,
                marker = marker
            });
            _sceneHandler.SideTowerPickerView.Hide();
            Object.Destroy(marker.gameObject);
        }
    }
}
