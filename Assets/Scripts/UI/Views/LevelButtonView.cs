using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class LevelButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image nodeBackground;
        [SerializeField] private TMP_Text levelNumberLabel;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject currentHighlight;
        [SerializeField] private Image[] starIcons;
        [SerializeField] private Sprite filledStarSprite;
        [SerializeField] private Sprite emptyStarSprite;
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color unlockedColor = Color.white;
        [SerializeField] private TMP_Text lockedReasonLabel;

        [Header("Подарок за первое прохождение (LevelDefinition.unlockRewardItem)")]
        [Tooltip("Контейнер значка. Прячется целиком, если на уровне подарка нет или он уже получен.")]
        [SerializeField] private GameObject rewardBadge;
        [SerializeField] private Image rewardIcon;
        [Tooltip("Затемнение значка на ещё закрытом уровне — подарок видно, но понятно, что он не получен.")]
        [SerializeField] private Color rewardLockedTint = new Color(1f, 1f, 1f, 0.55f);

        public Button Button => button;

        /// <summary>Узел уровня, на котором сейчас стоит игрок. Аниматор подсвечивает именно его.</summary>
        public bool IsNext { get; private set; }

        public void Setup(int levelNumber, bool unlocked, bool isNext, int stars, string lockedReasonText)
        {
            IsNext = unlocked && isNext;

            if (levelNumberLabel != null)
                levelNumberLabel.text = levelNumber.ToString();

            if (lockedOverlay != null)
                lockedOverlay.SetActive(!unlocked);

            if (currentHighlight != null)
                currentHighlight.SetActive(unlocked && isNext);

            if (nodeBackground != null)
                nodeBackground.color = unlocked ? unlockedColor : lockedColor;

            button.interactable = unlocked;

            for (var i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null)
                    continue;

                starIcons[i].gameObject.SetActive(true);
                starIcons[i].sprite = unlocked && i < stars ? filledStarSprite : emptyStarSprite;
            }

            if (lockedReasonLabel != null)
            {
                lockedReasonLabel.gameObject.SetActive(!unlocked);
                lockedReasonLabel.text = lockedReasonText;
            }
        }

        /// <summary>
        /// Значок подарка, который уровень выдаёт за первое прохождение. Скрыт, если подарка на
        /// уровне нет или игрок уже владеет предметом: LevelRewardService.GrantUnlockItem в этом
        /// случае ничего не выдаст (предмет мог быть куплен в магазине), и обещать его нельзя.
        /// </summary>
        public void SetupReward(Sprite rewardSprite, bool alreadyOwned, bool unlocked)
        {
            var show = rewardSprite != null && !alreadyOwned;

            if (rewardBadge != null)
                rewardBadge.SetActive(show);

            if (rewardIcon == null)
                return;

            rewardIcon.enabled = show;
            if (!show)
                return;

            rewardIcon.sprite = rewardSprite;
            rewardIcon.color = unlocked ? Color.white : rewardLockedTint;
        }
    }
}
