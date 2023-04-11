using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MenuScene
{
    public class Menu : MonoBehaviour
    {
        [SerializeField] private Button playBtn;
        [SerializeField] private Button exitBtn;

        private void Start()
        {
            InitializeBtns();
        }

        private void InitializeBtns()
        {
            playBtn.onClick.AddListener(delegate
            {
                SceneManager.LoadScene("GameScene");
            });
            
            exitBtn.onClick.AddListener(delegate
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;        
#else  
                Application.Quit();    
#endif
            });
        }
    }
}