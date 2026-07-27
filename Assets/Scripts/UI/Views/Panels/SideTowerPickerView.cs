using System;
using System.Collections.Generic;
using Db;
using UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Panels
{
    /// <summary>
    /// Компактный попап выбора доп-башни, появляется рядом с местом клика (над слотом)
    /// и закрывается по клику в любом другом месте экрана.
    /// </summary>
    public class SideTowerPickerView : MonoBehaviour
    {
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Button outsideClickCatcher;
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private SideTowerOptionButtonView optionButtonPrefab;
        [SerializeField] private Vector2 offsetFromPoint = new(0f, 120f);
        [SerializeField] private float screenEdgeMargin = 40f;

        private readonly List<GameObject> _spawnedOptions = new();
        private Action<SideTowerDefinition> _onPicked;

        private void Awake()
        {
            outsideClickCatcher.onClick.AddListener(Hide);
            gameObject.SetActive(false);
        }

        public void Show(Vector3 worldPosition, Camera worldCamera, IReadOnlyList<SideTowerDefinition> definitions,
            Action<SideTowerDefinition> onPicked)
        {
            _onPicked = onPicked;
            ClearOptions();

            foreach (var definition in definitions)
            {
                var option = Instantiate(optionButtonPrefab, optionsContainer);
                option.Setup(definition, () => _onPicked?.Invoke(definition));
                _spawnedOptions.Add(option.gameObject);
            }

            gameObject.SetActive(true);
            PositionAt(worldPosition, worldCamera);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearOptions();
        }

        private void PositionAt(Vector3 worldPosition, Camera worldCamera)
        {
            var screenPoint = worldCamera.WorldToScreenPoint(worldPosition);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out var localPoint);
            popupPanel.anchoredPosition = ClampToScreen(localPoint + offsetFromPoint);
        }

        /// <summary>
        /// Не даёт попапу выйти за пределы видимой области, с небольшим отступом от краёв.
        /// Учитывает реальный размер и pivot popupPanel, так что верстку можно свободно менять.
        /// </summary>
        private Vector2 ClampToScreen(Vector2 position)
        {
            var canvasBounds = canvasRect.rect;
            var size = popupPanel.rect.size;
            var pivot = popupPanel.pivot;

            var minX = canvasBounds.xMin + screenEdgeMargin + size.x * pivot.x;
            var maxX = canvasBounds.xMax - screenEdgeMargin - size.x * (1f - pivot.x);
            var minY = canvasBounds.yMin + screenEdgeMargin + size.y * pivot.y;
            var maxY = canvasBounds.yMax - screenEdgeMargin - size.y * (1f - pivot.y);

            return new Vector2(
                minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : (minX + maxX) / 2f,
                minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : (minY + maxY) / 2f);
        }

        private void ClearOptions()
        {
            foreach (var go in _spawnedOptions)
                Destroy(go);

            _spawnedOptions.Clear();
        }
    }
}
