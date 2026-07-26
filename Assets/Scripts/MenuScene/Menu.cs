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
            
#if UNITY_WEBGL && !UNITY_EDITOR
            // В WebGL Application.Quit() ничего не делает — мёртвая кнопка, модерация Яндекса такое режет.
            exitBtn.gameObject.SetActive(false);
#else
            exitBtn.onClick.AddListener(delegate
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
#endif
        }
    }
}