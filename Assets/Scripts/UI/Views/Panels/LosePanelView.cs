using Installers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI.Views.Panels
{
    public class LosePanelView : MonoBehaviour
    {
        [SerializeField] private Button restartBtn;
        [SerializeField] private Button exitBtn;
        
        private void Start()
        {
            restartBtn.onClick.AddListener(delegate
            {
                DiContainerRef.Container.UnbindAll();
                var loader = SceneManager.LoadSceneAsync("GameScene");

                loader.allowSceneActivation = true;
            });
            
            exitBtn.onClick.AddListener(delegate
            {
                var loader = SceneManager.LoadSceneAsync("Menu");

                loader.allowSceneActivation = true;
            });
        }
    }
}