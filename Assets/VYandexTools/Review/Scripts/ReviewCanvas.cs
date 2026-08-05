using System;
using Installers;
using Services;
using UnityEngine;
using UnityEngine.UI;

namespace VYandexTools.Review.Scripts
{
    public class ReviewCanvas : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button reviewButton;

        public event Action OnReviewButtonClick;
        public event Action OnCloseButtonClick;

        public bool IsOpened => root.activeSelf;

        private void Start()
        {
            closeButton.onClick.AddListener(CloseButton);
            reviewButton.onClick.AddListener(ReviewButton);
        }

        public void Show()
        {
            root.SetActive(true);
            DiContainerRef.Container?.TryResolve<IGameTimeProvider>()?.Pause();
        }

        public void Hide()
        {
            root.SetActive(false);
            DiContainerRef.Container?.TryResolve<IGameTimeProvider>()?.Resume();
        }

        private void ReviewButton()
        {
            OnReviewButtonClick?.Invoke();
        }

        private void CloseButton()
        {
            Hide();
            OnCloseButtonClick?.Invoke();
        }
    }
}
