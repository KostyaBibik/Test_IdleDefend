using System;

namespace Yandex
{
    public static class Advertisement
    {
        public static void ShowInterstitial(Action onOpenCallback = null, Action onCloseCallback = null,
            Action<string> onErrorCallback = null, Action onOfflineCallback = null, string placement = "")
        {
            if (SaveSystem.SaveData.NoAds || !Kimicu.YandexGames.Advertisement.AdvertisementIsAvailable)
            {
                onCloseCallback?.Invoke();
                return;
            }

            Kimicu.YandexGames.Advertisement.ShowInterstitialAd(() =>
                {
                    onOpenCallback?.Invoke();
                    GaEventProvider.InterstitialAdEvent(placement);
                }, onCloseCallback, onErrorCallback,
                onOfflineCallback);
        }

        public static void ShowReward(Action onOpenCallback = null, Action onRewardedCallback = null,
            Action onCloseCallback = null, Action<string> onErrorCallback = null, string placement = "")
        {
            Kimicu.YandexGames.Advertisement.ShowVideoAd(onOpenCallback, () =>
                {
                    onRewardedCallback?.Invoke();
                    GaEventProvider.RewardAdEvent(placement);
                }, onCloseCallback,
                onErrorCallback);
        }
    }
}