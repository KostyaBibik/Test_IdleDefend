using System;
using System.Collections;
using Kimicu.YandexGames;
using TMPro;
using UnityEngine;

namespace Yandex
{
    public class AdTimer : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;
        [SerializeField] private TMP_Text timerText;

        private const float WaitTime = 2f;

        private IEnumerator Start()
        {
            if (SaveSystem.SaveData.NoAds)
                yield break;

            while (true)
            {
                yield return new WaitForSeconds(120f);
                if (SaveSystem.SaveData.NoAds)
                    yield break;

                Time.timeScale = 0;
                AudioListener.volume = 0;
                AudioListener.pause = true;
                

                Show();
                float elapsedTime = WaitTime;
                bool isClosed = false;
                while (elapsedTime > 0)
                {
                    elapsedTime -= Time.unscaledDeltaTime;
                    timerText.text = $"Реклама через {elapsedTime:F1}";
                    yield return null;
                }

                Advertisement.ShowInterstitial(onCloseCallback: () =>
                {
                    isClosed = true;
                    Hide();
                });
#if UNITY_EDITOR
                Time.timeScale = 1;
#endif

                yield return new WaitUntil(() => isClosed);
            }
        }

        private void Show()
        {
            group.alpha = 1;
            group.blocksRaycasts = true;
        }

        private void Hide()
        {
            group.alpha = 0;
            group.blocksRaycasts = false;
        }
    }
}