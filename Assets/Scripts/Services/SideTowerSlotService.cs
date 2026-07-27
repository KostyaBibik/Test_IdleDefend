using Db;
using Helpers;
using Infrastructure.Impl;
using System.Collections.Generic;
using UI.Views;
using UnityEngine;
using Zenject;

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

        public SideTowerSlotService(
            LevelService levelService,
            SceneHandler sceneHandler,
            SideTowerCatalogConfig catalog,
            CoinService coinService,
            EntityFactory entityFactory,
            Camera camera
        )
        {
            _levelService = levelService;
            _sceneHandler = sceneHandler;
            _catalog = catalog;
            _coinService = coinService;
            _entityFactory = entityFactory;
            _camera = camera;
        }

        public void Initialize()
        {
            var indices = _levelService.CurrentLevel.UnlockedSideTowerSlotIndices;
            if (indices == null)
                return;

            var markers = _sceneHandler.SideTowerSlotMarkers;

            foreach (var index in indices)
            {
                if (index < 0 || markers == null || index >= markers.Length)
                    continue;

                SpawnMarker(markers[index].position);
            }
        }

        private void SpawnMarker(Vector3 position)
        {
            var marker = Object.Instantiate(_catalog.SlotMarkerPrefab, position, Quaternion.identity);
            marker.SetEventCamera(_camera);
            marker.Button.onClick.AddListener(() => OnMarkerClicked(marker));
        }

        private void OnMarkerClicked(SideTowerSlotMarkerView marker)
        {
            _sceneHandler.SideTowerPickerView.Show(marker.transform.position, _camera, _catalog.Definitions,
                definition => TryPurchase(marker, definition));
        }

        private void TryPurchase(SideTowerSlotMarkerView marker, SideTowerDefinition definition)
        {
            if (!_coinService.TryBought(definition.Cost))
                return;

            _entityFactory.CreateSideTower(marker.transform.position, definition);
            _sceneHandler.SideTowerPickerView.Hide();
            Object.Destroy(marker.gameObject);
        }
    }
}
