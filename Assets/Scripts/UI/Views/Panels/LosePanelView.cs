using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Views.Panels
{
    public class LosePanelView : MonoBehaviour
    {
        [SerializeField] private Button restartBtn;

        private void Start()
        {
            restartBtn.onClick.AddListener(delegate
            {
                SceneManager.LoadScene(0);
            });
        }
    }
}