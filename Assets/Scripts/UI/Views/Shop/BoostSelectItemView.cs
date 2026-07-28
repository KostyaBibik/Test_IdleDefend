using Db;
using Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Shop
{
    /// <summary>
    /// Строка буста на предбоевом экране: показывает иконку/название/количество и переключает
    /// выбор буста на следующий бой через BoostInventoryService. Ничего не списывает сама -
    /// списание бустов происходит в ActiveBoostService при старте GameScene.
    /// </summary>
    public class BoostSelectItemView : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text countLabel;
        [SerializeField] private GameObject selectedState;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField, Range(0f, 1f)] private float disabledAlpha = 0.4f;

        private ShopItemDefinition _item;

        public void Setup(ShopItemDefinition item)
        {
            _item = item;

            gameObject.SetActive(item != null);

            if (item == null)
                return;

            if (icon != null)
                icon.sprite = item.Icon;

            if (nameLabel != null)
                nameLabel.text = item.DisplayName;

            BindButton();
            Refresh();
        }

        public void Refresh()
        {
            if (_item == null)
                return;

            var count = BoostInventoryService.GetCount(_item);
            var available = count > 0;
            var selected = BoostInventoryService.IsSelected(_item);

            if (countLabel != null)
                countLabel.text = $"x{count}";

            if (selectedState != null)
                selectedState.SetActive(selected);

            if (toggleButton != null)
                toggleButton.interactable = available;

            if (canvasGroup != null)
                canvasGroup.alpha = available ? 1f : disabledAlpha;
        }

        private void BindButton()
        {
            if (toggleButton == null)
                return;

            toggleButton.onClick.RemoveListener(NotifyToggled);
            toggleButton.onClick.AddListener(NotifyToggled);
        }

        private void NotifyToggled()
        {
            if (_item == null)
                return;

            BoostInventoryService.ToggleSelected(_item);
            Refresh();
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(NotifyToggled);
        }
    }
}
