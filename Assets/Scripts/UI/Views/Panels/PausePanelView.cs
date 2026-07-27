using UnityEngine;
using UnityEngine.UI;

namespace UI.Views.Panels
{
    public class PausePanelView : MonoBehaviour
    {
        [SerializeField] private Button continueBtn;

        private void Awake()
        {
            continueBtn.onClick.AddListener(Close);
            gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }
    }
}
