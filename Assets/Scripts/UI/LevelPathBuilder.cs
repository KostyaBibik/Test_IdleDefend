using System;
using Db;
using Game.Localization;
using Services;
using UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LevelPathBuilder : MonoBehaviour
    {
        [SerializeField] private RectTransform content;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private LevelButtonView nodePrefab;
        [SerializeField] private RectTransform connectorPrefab;
        [SerializeField] private float verticalSpacing = 260f;
        [SerializeField] private float horizontalAmplitude = 220f;
        [SerializeField] private float bottomPadding = 220f;
        [SerializeField] private float topPadding = 320f;
        [SerializeField] private float connectorThickness = 18f;

        public void Build(LevelsConfig levelsConfig, Action<int> onLevelSelected)
        {
            for (var i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            var levelCount = levelsConfig.Count;
            if (levelCount <= 0)
                return;

            var unlockedIndex = SaveSystem.GetUnlockedLevelIndex();
            var positions = new Vector2[levelCount];

            for (var i = 0; i < levelCount; i++)
            {
                var x = Mathf.Sin(i * 1.15f) * horizontalAmplitude;
                var yFromTop = topPadding + (levelCount - 1 - i) * verticalSpacing;
                positions[i] = new Vector2(x, -yFromTop);
            }

            for (var i = 0; i < levelCount - 1; i++)
                CreateConnector(positions[i], positions[i + 1]);

            for (var i = 0; i < levelCount; i++)
            {
                var level = levelsConfig.GetByIndex(i);
                var node = Instantiate(nodePrefab, content);
                var nodeRect = node.GetComponent<RectTransform>();
                nodeRect.anchorMin = new Vector2(0.5f, 1f);
                nodeRect.anchorMax = new Vector2(0.5f, 1f);
                nodeRect.anchoredPosition = positions[i];

                var unlocked = i <= unlockedIndex;
                var isNext = i == unlockedIndex;
                var stars = SaveSystem.GetLevelStars(level.LevelId);
                var lockedReasonText = unlocked
                    ? null
                    : GameLocalization.Format(LocalizationKey.locked_complete_stage_format, "Complete stage {0}", i);

                node.Setup(i + 1, unlocked, isNext, stars, lockedReasonText);

                var levelIndex = i;
                node.Button.onClick.AddListener(delegate { onLevelSelected(levelIndex); });
            }

            content.sizeDelta = new Vector2(content.sizeDelta.x, topPadding + (levelCount - 1) * verticalSpacing + bottomPadding);

            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void CreateConnector(Vector2 from, Vector2 to)
        {
            var connector = Instantiate(connectorPrefab, content);
            connector.anchorMin = new Vector2(0.5f, 1f);
            connector.anchorMax = new Vector2(0.5f, 1f);
            var dir = to - from;
            var distance = dir.magnitude;
            var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            connector.anchoredPosition = from;
            connector.sizeDelta = new Vector2(distance, connectorThickness);
            connector.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
