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
        [SerializeField] private ClickThroughOverlay outsideClickCatcher;
        [SerializeField] private RectTransform popupPanel;
        [SerializeField] private RectTransform optionsContainer;
        [SerializeField] private GridLayoutGroup optionsGridLayout;
        [SerializeField] private SideTowerOptionButtonView optionButtonPrefab;
        [SerializeField] private Vector2 offsetFromPoint = new(0f, 120f);
        [SerializeField] private float screenEdgeMargin = 40f;
        [SerializeField] private int maxColumns = 3;
        [SerializeField] private Vector2 panelPadding = new(40f, 40f);

        private readonly List<SideTowerOptionButtonView> _spawnedOptions = new();
        private Action<SideTowerDefinition> _onPicked;

        private void Awake()
        {
            outsideClickCatcher.Closed += Hide;
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
                _spawnedOptions.Add(option);
            }

            ResizeForOptionCount(definitions.Count);

            gameObject.SetActive(true);
            PositionAt(worldPosition, worldCamera);
        }

        /// <summary>
        /// Подгоняет размер сетки и попапа под текущее число вариантов, чтобы ряд не растягивался
        /// шире экрана — с ростом каталога башен опции переносятся на новую строку, а попап растёт
        /// вниз, а не вширь.
        /// </summary>
        private void ResizeForOptionCount(int count)
        {
            var columns = Mathf.Clamp(count, 1, maxColumns);
            var rows = Mathf.Max(1, Mathf.CeilToInt(count / (float) maxColumns));

            var cellSize = optionsGridLayout.cellSize;
            var spacing = optionsGridLayout.spacing;

            var width = columns * cellSize.x + (columns - 1) * spacing.x;
            var height = rows * cellSize.y + (rows - 1) * spacing.y;

            optionsContainer.sizeDelta = new Vector2(width, height);
            popupPanel.sizeDelta = new Vector2(width + panelPadding.x, height + panelPadding.y);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            ClearOptions();
        }

        public SideTowerOptionButtonView GetOptionFor(SideTowerDefinition definition)
        {
            foreach (var option in _spawnedOptions)
            {
                if (option != null && option.Definition == definition)
                    return option;
            }

            return null;
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
            foreach (var option in _spawnedOptions)
            {
                if (option != null)
                    Destroy(option.gameObject);
            }

            _spawnedOptions.Clear();
        }
    }
}
