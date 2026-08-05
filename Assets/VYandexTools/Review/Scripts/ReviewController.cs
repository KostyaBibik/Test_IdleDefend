using System.Collections;
using Kimicu.YandexGames;
using Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VYandexTools.Review.Scripts
{
    public class ReviewController : MonoBehaviour
    {
        private const string MenuSceneName = "Menu";

        [SerializeField] private ReviewCanvas reviewCanvas;
        [SerializeField] private bool useTimer = true;
        [SerializeField] private float time = 300;
        [SerializeField] private int rewardEmeralds = 1000;

        public static ReviewController Instance;

        private Coroutine _timer;

        // Таймер только помечает готовность — реальный показ ждёт подходящего момента
        // (сцена Menu или конец игровой сессии), чтобы не прерывать бой.
        private bool _pendingShow;
        private bool _requestInFlight;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            ReviewTriggerEvents.OnGameSessionEnded += OnGameSessionEnded;
        }

        private IEnumerator Start()
        {
            reviewCanvas.OnReviewButtonClick += ShowYandexReviewPopup;

            yield return new WaitUntil(() => YandexGamesSdk.IsInitialized);

            if (useTimer)
                _timer = StartCoroutine(Timer());
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MenuSceneName)
                TryOpenIfPending();
        }

        private void OnGameSessionEnded() => TryOpenIfPending();

        private void TryOpenIfPending()
        {
            if (!_pendingShow || _requestInFlight)
                return;

            TryShowReviewPopup();
        }

        public void TryShowReviewPopup()
        {
            if (_requestInFlight)
                return;

            _requestInFlight = true;
#if UNITY_EDITOR
            _pendingShow = false;
            _requestInFlight = false;
            reviewCanvas.Show();
#else
            Agava.YandexGames.ReviewPopup.CanOpen((b, s) =>
            {
                Debug.Log($"Review CanOpen: {b}, reason: {s}");
                _requestInFlight = false;
                _pendingShow = false;

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
            EmeraldWallet.Add(rewardEmeralds);
            reviewCanvas.Hide();
#else
            Agava.YandexGames.ReviewPopup.Open(result =>
            {
                if (result)
                {
                    EmeraldWallet.Add(rewardEmeralds);
                    Debug.Log("Награда за отзыв получена");
                }

                if (_timer != null) StopCoroutine(_timer);
                reviewCanvas.Hide();
                GaEventProvider.DesignEvent("Review", $"Result {result}".ToString);
            });
#endif
        }

        private IEnumerator Timer()
        {
            while (true)
            {
                yield return new WaitForSeconds(time);
                _pendingShow = true;
                yield return new WaitUntil(() => reviewCanvas.IsOpened);
                yield return new WaitUntil(() => !reviewCanvas.IsOpened);
            }
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            ReviewTriggerEvents.OnGameSessionEnded -= OnGameSessionEnded;
        }
    }
}
