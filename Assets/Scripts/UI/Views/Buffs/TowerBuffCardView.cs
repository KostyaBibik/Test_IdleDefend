using System;
using Db;
using Enums;
using Game.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Buffs
{
    /// <summary>
    /// Карточка одного варианта бафа в окне левел-апа башни. Оформление зависит от редкости.
    /// </summary>
    public class TowerBuffCardView : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private GameObject legendaryGlow;
        [SerializeField] private Button selectButton;

        [SerializeField]
        private RarityStyle[] rarityStyles =
        {
            new RarityStyle { rarity = ETowerBuffRarity.Common },
            new RarityStyle { rarity = ETowerBuffRarity.Rare },
            new RarityStyle { rarity = ETowerBuffRarity.Legendary },
        };

        private TowerBuffDefinition _buff;
        private Action<TowerBuffDefinition> _onSelected;

        public TowerBuffDefinition Buff => _buff;
        public Button SelectButton => selectButton;

        [Serializable]
        private struct RarityStyle
        {
            public ETowerBuffRarity rarity;
            public Sprite backgroundSprite;
        }

        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(NotifySelected);
        }

        public void Setup(TowerBuffDefinition buff, Action<TowerBuffDefinition> onSelected)
        {
            _buff = buff;
            _onSelected = onSelected;

            if (iconImage != null)
            {
                iconImage.sprite = buff.Icon;
                iconImage.enabled = buff.Icon != null;
            }

            if (nameText != null)
                nameText.text = GameLocalization.TowerBuffName(buff);

            if (descriptionText != null)
                descriptionText.text = GameLocalization.TowerBuffDescription(buff);

            ApplyRarity(buff.Rarity);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _buff = null;
            _onSelected = null;
            gameObject.SetActive(false);
        }

        private void ApplyRarity(ETowerBuffRarity rarity)
        {
            foreach (var style in rarityStyles)
            {
                if (style.rarity != rarity)
                    continue;

                if (backgroundImage != null && style.backgroundSprite != null)
                    backgroundImage.sprite = style.backgroundSprite;

                break;
            }

            if (legendaryGlow != null)
                legendaryGlow.SetActive(rarity == ETowerBuffRarity.Legendary);
        }

        private void NotifySelected()
        {
            if (_buff != null)
                _onSelected?.Invoke(_buff);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(NotifySelected);
        }
    }
}
