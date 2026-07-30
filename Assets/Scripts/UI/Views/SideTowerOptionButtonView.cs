using System;
using Db;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class SideTowerOptionButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text costLabel;

        public SideTowerDefinition Definition { get; private set; }
        public Button Button => button;

        public void Setup(SideTowerDefinition definition, Action onClick)
        {
            Definition = definition;
            if (icon != null)
                icon.sprite = definition.Icon;

            nameLabel.text = GameLocalization.SideTowerName(definition);
            costLabel.text = definition.Cost.ToString();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
