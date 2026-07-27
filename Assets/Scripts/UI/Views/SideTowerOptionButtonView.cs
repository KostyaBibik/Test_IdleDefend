using System;
using Db;
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

        public void Setup(SideTowerDefinition definition, Action onClick)
        {
            if (icon != null)
                icon.sprite = definition.Icon;

            nameLabel.text = definition.DisplayName;
            costLabel.text = definition.Cost.ToString();

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick());
        }
    }
}
