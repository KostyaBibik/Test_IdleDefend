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

        public Button Button => button;

        public void Setup(int levelNumber, bool unlocked, bool isNext, int stars, string lockedReasonText)
        {
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
    }
}
