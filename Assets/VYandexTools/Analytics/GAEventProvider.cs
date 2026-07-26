using System.Collections;
using System.Linq;
using GameAnalyticsSDK;
using UnityEngine;

public static class GaEventProvider
{
    public static IEnumerator FirstClickCheck()
    {
        yield return new WaitUntil(() => Input.GetMouseButton(0));
        
        DesignEvent("Game Started");
    }
    
    public static void DesignEvent(string key, params MetricData[] metricData)
    {
        if (metricData != null && metricData.Length > 0)
            key += ":" + string.Join(":", metricData.Select(x => x.Invoke()));
        
        GameAnalytics.NewDesignEvent(key);
    }

    public static void ProgressionEvent(GAProgressionStatus progressionStatus, params MetricData[] metricData)
    {
        switch(metricData.Length)
        {
            case 1:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke());
                break;
            case 2:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke(), metricData[1].Invoke());
                break;
            case 3:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke(), metricData[1].Invoke(), metricData[2].Invoke());
                break;
            case 4:
                GameAnalytics.NewProgressionEvent(progressionStatus, metricData[0].Invoke(), metricData[1].Invoke(), metricData[3].Invoke());
                break;
        }
    }

    public static void RewardAdEvent(string placement)
    {
        GameAnalytics.NewAdEvent(GAAdAction.RewardReceived, GAAdType.RewardedVideo,"Yandex", placement);
    }

    public static void InterstitialAdEvent(string placement)
    {
        GameAnalytics.NewAdEvent(GAAdAction.Show, GAAdType.Interstitial, "Yandex", placement);
    }

    public delegate string MetricData();
}