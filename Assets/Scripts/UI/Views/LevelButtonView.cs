using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Views
{
    public class LevelButtonView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text levelNumberLabel;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private GameObject[] starIcons;

        public Button Button => button;

        public void Setup(int levelNumber, bool unlocked, int stars)
        {
            if (levelNumberLabel != null)
                levelNumberLabel.text = levelNumber.ToString();

            if (lockedOverlay != null)
                lockedOverlay.SetActive(!unlocked);

            button.interactable = unlocked;

            for (var i = 0; i < starIcons.Length; i++)
                starIcons[i].SetActive(unlocked && i < stars);
        }
    }
}
