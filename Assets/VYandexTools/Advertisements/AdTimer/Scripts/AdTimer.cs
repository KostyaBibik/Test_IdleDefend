using System;
using System.Collections;
using Game.Localization;
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
        private const string AdPlacement = "timer";

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
                    timerText.text = GameLocalization.Format(LocalizationKey.ad_timer_format, "Реклама через {0:F1}", elapsedTime);
                    yield return null;
                }

                // placement обязателен: без него ad-событие уходит в GA с пустой строкой и
                // периодические показы неразличимы от всех остальных в отчётах.
                Advertisement.ShowInterstitial(onCloseCallback: () =>
                {
                    isClosed = true;
                    Hide();
                }, placement: AdPlacement);
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