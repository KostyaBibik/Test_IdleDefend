using System;
using System.Collections;
using Kimicu.YandexGames;
using UnityEngine;

namespace VYandexTools.Review.Scripts
{
    public class ReviewController : MonoBehaviour
    {
        [SerializeField] private ReviewCanvas reviewCanvas;
        [SerializeField] private bool useTimer = true;
        [SerializeField] private float time = 300;

        public static ReviewController Instance;

        private Coroutine _timer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
                Destroy(gameObject);
        }

        private IEnumerator Start()
        {
            reviewCanvas.OnReviewButtonClick += ShowYandexReviewPopup;

            yield return new WaitUntil(() => YandexGamesSdk.IsInitialized);

            if (useTimer)
                _timer = StartCoroutine(Timer(TryShowReviewPopup));
        }

        public void TryShowReviewPopup()
        {
#if UNITY_EDITOR
            reviewCanvas.Show();
#else
            Agava.YandexGames.ReviewPopup.CanOpen((b, s) =>
            {
                Debug.Log($"Review CanOpen: {b}, reason: {s}");
                if (b == false)
                {
                    if (_timer != null) StopCoroutine(_timer);
                    return;
                }

                reviewCanvas.Show();
            });
#endif
        }

        private void ShowYandexReviewPopup()
        {
#if UNITY_EDITOR
            if (_timer != null) StopCoroutine(_timer);
            reviewCanvas.Hide();
#else
            Agava.YandexGames.ReviewPopup.Open(result =>
            {
                if (result)
                {
                    //TODO: Начисление награды
                    Debug.Log("Награда за отзыв получена");
                }

                if (_timer != null) StopCoroutine(_timer);
                reviewCanvas.Hide();
                GaEventProvider.DesignEvent("Review", $"Result {result}".ToString);
            });
#endif
        }

        private IEnumerator Timer(Action callback)
        {
            while (true)
            {
                yield return new WaitForSeconds(time);
                callback.Invoke();
                yield return new WaitUntil(() => reviewCanvas.IsOpened);
                yield return new WaitUntil(() => !reviewCanvas.IsOpened);
            }
        }
    }
}
