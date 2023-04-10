using TMPro;
using UnityEngine;

namespace UI.Views.Game
{
    public class RewardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private RectTransform rectTransform;

        public TMP_Text Text => text;
        public RectTransform RectTransform => rectTransform;
    }
}