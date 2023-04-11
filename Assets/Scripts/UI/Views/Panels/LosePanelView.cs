using Installers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace UI.Views.Panels
{
    public class LosePanelView : MonoBehaviour
    {
        [SerializeField] private Button restartBtn;

        private void Start()
        {
            restartBtn.onClick.AddListener(delegate
            {
                DiContainerRef.Container.UnbindAll();
                var loader = SceneManager.LoadSceneAsync("GameScene");

                loader.allowSceneActivation = true;
            });
        }
    }
}