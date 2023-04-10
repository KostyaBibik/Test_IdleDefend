using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Game
{
    public class TowerHealthView : MonoBehaviour
    {
        [SerializeField] private Image image;

        public Image Image => image;
    }
}