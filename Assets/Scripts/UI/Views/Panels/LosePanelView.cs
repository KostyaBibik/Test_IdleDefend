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

        private bool _transitionStarted;
        private bool _restartStarted;

        private void Start()
        {
            restartBtn.onClick.AddListener(delegate
            {
                if (_transitionStarted)
                    return;
                _transitionStarted = true;
                restartBtn.interactable = false;

                // Логическая пауза между забегами — штатное место для полноэкранной рекламы.
                // Обёртка сама проверяет NoAds/доступность и зовёт onClose, если показа не было.
                // Ошибка/офлайн тоже должны вести в рестарт, иначе кнопка останется мёртвой.
                Yandex.Advertisement.ShowInterstitial(
                    onCloseCallback: RestartGame,
                    onErrorCallback: _ => RestartGame(),
                    onOfflineCallback: RestartGame,
                    placement: "restart");
            });

            exitBtn.onClick.AddListener(delegate
            {
                if (_transitionStarted)
                    return;
                _transitionStarted = true;

                var loader = SceneManager.LoadSceneAsync("Menu");

                loader.allowSceneActivation = true;
            });
        }

        private void RestartGame()
        {
            if (_restartStarted)
                return;
            _restartStarted = true;

            DiContainerRef.Container.UnbindAll();
            var loader = SceneManager.LoadSceneAsync("GameScene");

            loader.allowSceneActivation = true;
        }
    }
}
